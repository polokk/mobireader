using System;
using System.Collections.Generic;
using System.Text;

namespace MobiReader
{
	public class MobiFile :  PalmFile
	{
		protected UInt32 m_EncryptionType;
		protected UInt32 m_HuffOffset;
		protected UInt32 m_HuffCount;
		protected UInt32 m_Extra_flags;
		protected String m_BookText;

		public UInt32 EncryptionType {
			get { return m_EncryptionType; }
		}

		public String BookText {
			get { return m_BookText; }
		}

		public MobiFile () : base ()
		{

		}

		public MobiFile (PalmFile pf)
			: base ()
		{
			this.m_AppInfoID = pf.AppInfoID;
			this.m_Attributes = pf.m_Attributes;
			this.m_Compression = pf.Compression;
			this.m_CreationDate = pf.CreationDate;
			this.m_Creator = pf.Creator;
			this.m_CurrentPosition = pf.CurrentPosition;
			this.m_FileName = pf.FileName;
			this.m_LastBackupDate = pf.LastBackupDate;
			this.m_ModificationDate = pf.ModificationDate;
			this.m_ModificationNumber = pf.ModificationNumber;
			this.m_Name = pf.Name;
			this.m_NextRecordListID = pf.NextRecordListID;
			this.m_NumberOfRecords = pf.NumberOfRecords;
			this.m_RecordList = pf.RecordList;
			this.m_SortInfoID = pf.SortInfoID;
			this.m_TextLength = pf.TextLength;
			this.m_TextRecordCount = pf.TextRecordCount;
			this.m_TextRecordSize = pf.TextRecordSize;
			this.m_Type = pf.Type;
			this.m_UniqueIDSeed = pf.UniqueIDSeed;
			this.m_Version = pf.Version;
			this.m_RecordInfoList = pf.m_RecordInfoList;
		}

		public new static MobiFile LoadFile (String fileName)
		{
			MobiFile retval = new MobiFile (PalmFile.LoadFile (fileName));
			List<Byte> empty2 = new List<Byte> ();
			List<Byte> temp = new List<Byte> ();
			empty2.Add (0);
			empty2.Add (0);
			List<Byte> headerdata = new List<Byte> ();
			headerdata.AddRange (retval.m_RecordList [0].Data);

			temp.AddRange (empty2);
			temp.AddRange (headerdata.GetRange (12, 2));
			retval.m_EncryptionType = BytesToUint (temp.ToArray ());

			if (retval.Compression == 2) {
				StringBuilder sb = new StringBuilder ();
				Int32 pos = 0;
				Int32 a = 1;
				List<Byte> datatemp = null;
				while (a < retval.m_TextRecordCount + 1) {
					List<Byte> blockbuilder = new List<Byte> ();
					datatemp = new List<byte> (retval.m_RecordList [a++].Data);
					datatemp.Add (0);
					pos = 0;
					List<Byte> temps = new List<Byte> ();

					while (pos < datatemp.Count && blockbuilder.Count < 4096) {

						Byte ab = (byte)(datatemp[pos++] );
						if (ab == 0x00 || (ab > 0x08 && ab <= 0x7f)) {
							blockbuilder.Add (ab);
							//blockbuilder.Add (0);
						} else if (ab > 0x00 && ab <= 0x08) {
							temps.Clear ();
							temps.Add (0);
							temps.Add (0);
							temps.Add (0);
							temps.Add (ab);
							UInt32 value = BytesToUint (temps.ToArray ());
							for (uint i = 0; i < value; i++) {
								blockbuilder.Add((byte)(datatemp [pos++] ));
							//	blockbuilder.Add (0);
							}


                            
						} else if (ab > 0x7f && ab <= 0xbf) {
                          
							temps.Clear ();
							temps.Add (0);
							temps.Add (0);
							Byte bb = (Byte)((ab & 63));  // do this to drop the first 2 bits
							temps.Add (bb);
							if (pos < datatemp.Count) {
								temps.Add ((byte)(datatemp [pos++] ));
							} else {
								temps.Add (0);
							}
				
							UInt32 b = BytesToUint (temps.ToArray ());
							UInt32 dist = (b >> 3)*1;
							UInt32 len = ((b << 29) >> 29);
							Int32 uncompressedpos = blockbuilder.Count - ((Int32)dist);
							for (int i = 0; i < (len + 3)*1; i++) { 
								try {
									blockbuilder.Add(blockbuilder[uncompressedpos + i]);
								} catch (Exception) {
								}
							}
							
                           
						} else if (ab > 0xbf && ab <= 0xff) {
							blockbuilder.Add (32);
							//blockbuilder.Add (0);
							blockbuilder.Add((byte)(ab ^ 0x80));
							//blockbuilder.Add (0);
						}
					}

					sb.Append (Encoding.UTF8.GetString(blockbuilder.ToArray()));
				}
				retval.m_BookText = sb.ToString ();

			} else if (retval.Compression == 17480) {
				temp.Clear ();
				temp.AddRange (headerdata.GetRange (112, 4));
				retval.m_HuffOffset = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (headerdata.GetRange (116, 4));
				retval.m_HuffCount = BytesToUint (temp.ToArray ());

				if (headerdata.Count >= 244) {
					temp.Clear ();
					temp.AddRange (headerdata.GetRange (240, 4));
					retval.m_Extra_flags = BytesToUint (temp.ToArray ());
				}

				UInt32 off1;
				UInt32 off2;
				UInt32 entrybits;
				List<Byte> huffdata = new List<Byte> ();
				List<Byte> cdicdata = new List<Byte> ();
				huffdata.AddRange (retval.m_RecordList [retval.m_HuffOffset].Data);
				cdicdata.AddRange (retval.m_RecordList [retval.m_HuffOffset + 1].Data);

				temp.Clear ();
				temp.AddRange (huffdata.GetRange (16, 4));
				off1 = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (huffdata.GetRange (20, 4));
				off2 = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (cdicdata.GetRange (12, 4));
				entrybits = BytesToUint (temp.ToArray ());

				List<UInt32> huffdict1 = new List<UInt32> ();
				List<UInt32> huffdict2 = new List<UInt32> ();
				List<List<Byte>> huffdicts = new List<List<Byte>> ();

				for (int i = 0; i < 256; i++) {
					temp.Clear ();
					temp.AddRange (huffdata.GetRange ((int)(off1 + (i * 4)), 4));
					huffdict1.Add (BitConverter.ToUInt32 (temp.ToArray (), 0));
				}
				for (int i = 0; i < 64; i++) {
					temp.Clear ();
					temp.AddRange (huffdata.GetRange ((int)(off2 + (i * 4)), 4));
					huffdict2.Add (BitConverter.ToUInt32 (temp.ToArray (), 0));
				}

				for (int i = 1; i < retval.m_HuffCount; i++) {
					huffdicts.Add (new List<byte> (retval.m_RecordList [retval.m_HuffOffset + i].Data));
				}

				StringBuilder sb = new StringBuilder ();
				for (int i = 0; i < retval.m_TextRecordCount; i++) {
					// Remove Trailing Entries
					List<Byte> datatemp = new List<byte> (retval.m_RecordList [1 + i].Data);
					Int32 size = getSizeOfTrailingDataEntries (datatemp.ToArray (), datatemp.Count, retval.m_Extra_flags);

					sb.Append (unpack (new BitReader (datatemp.GetRange (0, datatemp.Count - size).ToArray ()), huffdict1.ToArray (), huffdict2.ToArray (), huffdicts, (int)((long)entrybits)));
				}

				retval.m_BookText = sb.ToString ();
			} else {
				throw new Exception ("Compression format is unsupported");
			}
			return retval;
		}

		protected static String ret = "";

		protected static String unpack (BitReader bits, UInt32[] huffdict1, UInt32[] huffdict2, List<List<Byte>> huffdicts, Int32 entrybits)
		{
			return unpack (bits, 0, huffdict1, huffdict2, huffdicts, entrybits);
		}

		protected static String unpack (BitReader bits, Int32 depth, UInt32[] huffdict1, UInt32[] huffdict2, List<List<Byte>> huffdicts, Int32 entrybits)
		{

			StringBuilder retval = new StringBuilder ();

			if (depth > 32) {
				throw new Exception ("corrupt file");
			}
			while (bits.left ()) {
				UInt64 dw = bits.peek (32);
				UInt32 v = (huffdict1 [dw >> 24]);
				UInt32 codelen = v & 0x1F;
				//assert codelen != 0;
				UInt64 code = dw >> (int)(32 - codelen);
				UInt64 r = (v >> 8);
				if ((v & 0x80) == 0) {
					while (code < ((ulong)huffdict2 [(codelen - 1) * 2])) {
						codelen += 1;
						code = dw >> (int)(32 - codelen);
					}
					r = huffdict2 [(codelen - 1) * 2 + 1];
				}
				r -= code;
				//assert codelen != 0;
				if (!bits.eat (codelen)) {
					return retval.ToString ();
				}
				UInt64 dicno = r >> entrybits;
				UInt64 off1 = 16 + (r - (dicno << entrybits)) * 2;
				List<Byte> dic = huffdicts [(int)((long)dicno)];
				Int32 off2 = 16 + (char)(dic [(int)((long)off1)]) * 256 + (char)(dic [(int)((long)off1) + 1]);
				Int32 blen = ((char)(dic [off2]) * 256 + (char)(dic [off2 + 1]));
				List<Byte> slicelist = dic.GetRange (off2 + 2, (int)(blen & 0x7fff));
				Byte[] slice = slicelist.ToArray ();
				if ((blen & 0x8000) > 0) {
					retval.Append (System.Text.ASCIIEncoding.ASCII.GetString (slice));
				} else {
					retval.Append (unpack (new BitReader (slice), depth + 1, huffdict1, huffdict2, huffdicts, entrybits));
				}
			}
			return retval.ToString ();
		}

		protected static Int32 getSizeOfTrailingDataEntries (Byte[] ptr, Int32 size, UInt32 flags)
		{
			Int32 retval = 0;
			flags >>= 1;
			while (flags > 0) {
				if ((flags & 1) > 0) {
					retval += (int)((long)getSizeOfTrailingDataEntry (ptr, size - retval));
				}
				flags >>= 1;
			}
			return retval;
		}

		protected static UInt32 getSizeOfTrailingDataEntry (Byte[] ptr, Int32 size)
		{
			UInt32 retval = 0;
			Int32 bitpos = 0;
			while (true) {
				UInt32 v = (char)(ptr [size - 1]);
				retval |= (v & 0x7F) << bitpos;
				bitpos += 7;
				size -= 1;
				if ((v & 0x80) != 0 || (bitpos >= 28) || (size == 0)) {
					return retval;
				}
			}
		}
	}

	public class BitReader
	{
		List<Byte> m_data;
		UInt32 m_pos = 0;
		Int32 m_nbits;

		public BitReader (Byte[] bytes)
		{
			m_data = new List<byte> (bytes);
			m_data.Add (0);
			m_data.Add (0);
			m_data.Add (0);
			m_data.Add (0);
			m_nbits = (m_data.Count - 4) * 8;
		}

		public UInt64 peek (UInt64 n)
		{
			UInt64 r = 0;
			UInt64 g = 0;
			while (g < n) {
				r = (r << 8) | (char)(m_data [(int)((long)(m_pos + g >> 3))]);
				g = g + 8 - ((m_pos + g) & 7);
			}
			return (ulong)(r >> (int)((long)(g - n))) & (ulong)(((ulong)(1) << (int)n) - 1);
		}

		public Boolean eat (UInt32 n)
		{
			m_pos += n;
			return m_pos <= m_nbits;
		}

		public Boolean left ()
		{
			return (m_nbits - m_pos) > 0;
		}
	}
}
