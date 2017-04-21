using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MobiReader
{
	public struct PalmRecord
	{
		public Byte[] Data;
	}

	public struct PalmRecordInfo
	{
		public UInt32 DataOffset;
		public UInt32 Attributes;
		public UInt32 UniqueID;
	}

	public class PalmFile
	{
		protected String m_FileName;
		protected String m_Name;
		// Database Name
		internal UInt32 m_Attributes;
		// bit field.
		protected UInt32 m_Version;
		// File Version
		protected DateTime m_CreationDate;
		// Creation Date
		protected DateTime m_ModificationDate;
		// Modification Date
		protected DateTime m_LastBackupDate;
		// Last Backup Date
		protected UInt32 m_ModificationNumber;
		protected UInt32 m_AppInfoID;
		protected UInt32 m_SortInfoID;
		protected String m_Type;
		protected String m_Creator;
		protected UInt32 m_UniqueIDSeed;
		protected UInt32 m_NextRecordListID;
		protected UInt32 m_NumberOfRecords;
		protected UInt32 m_Compression;
		protected UInt32 m_TextLength;
		protected UInt32 m_TextRecordCount;
		protected UInt32 m_TextRecordSize;
		protected UInt32 m_CurrentPosition;

		internal PalmRecordInfo[] m_RecordInfoList;
		internal PalmRecord[] m_RecordList;

		public String Name {
			get { return m_Name; }
		}

		public String FileName {
			get { return m_FileName; }
		}

		public Boolean ReadOnly {
			get { return (m_Attributes & 0x0002) == 0x0002; }

		}

		public Boolean DirtyAppInfoArea {
			get { return (m_Attributes & 0x0004) == 0x0004; }

		}

		public Boolean BackupThisDatabase {
			get { return (m_Attributes & 0x0008) == 0x0008; }
		}

		public Boolean OKToInstallNewer {
			get { return (m_Attributes & 0x0010) == 0x0010; }
		}

		public Boolean ForceReset {
			get { return (m_Attributes & 0x0020) == 0x0020; }
		}

		public Boolean NoBeam {
			get { return (m_Attributes & 0x0040) == 0x0040; }
		}

		public UInt32 Version {
			get { return m_Version; }
		}

		public DateTime CreationDate {
			get { return m_CreationDate; }
		}

		public DateTime ModificationDate {
			get { return m_ModificationDate; }
		}

		public DateTime LastBackupDate {
			get { return m_LastBackupDate; }
		}

		public UInt32 ModificationNumber {
			get { return m_ModificationNumber; }
		}

		public UInt32 AppInfoID {
			get { return m_AppInfoID; }
		}

		public UInt32 SortInfoID {
			get { return m_SortInfoID; }
		}

		public String Type {
			get { return m_Type; }
		}

		public String Creator {
			get { return m_Creator; }
		}

		public UInt32 UniqueIDSeed {
			get { return m_UniqueIDSeed; }
		}

		public UInt32 NextRecordListID {
			get { return m_NextRecordListID; }
		}

		public UInt32 NumberOfRecords {
			get { return m_NumberOfRecords; }
		}

		public PalmRecord[] RecordList {
			get { return m_RecordList; }
		}

		public UInt32 Compression {
			get { return m_Compression; }
		}

		public UInt32 TextLength {
			get { return m_TextLength; }
		}

		public UInt32 TextRecordCount {
			get { return m_TextRecordCount; }
		}

		public UInt32 TextRecordSize {
			get { return m_TextRecordSize; }
		}

		public UInt32 CurrentPosition {
			get { return m_CurrentPosition; }
		}

		public PalmFile ()
		{

		}

		public static PalmFile LoadFile (String fileName)
		{
			PalmFile retval = new PalmFile ();
			retval.m_FileName = fileName;
			FileStream fs = null;
			StreamReader sr = null;
			UInt32 seconds = 0;
			DateTime startdate = new DateTime (1904, 1, 1);

			//startdate = new DateTime(1970, 1, 1);
			try {
				fs = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				sr = new StreamReader (fs);
				Char[] buffer = new char[32];
				sr.Read (buffer, 0, 32);
				fs.Seek (32, SeekOrigin.Begin);
				retval.m_Name = new String (buffer);
				Byte[] bytebuffer = new Byte[4];
				fs.Read (bytebuffer, 2, 2);
				retval.m_Attributes = BytesToUint (bytebuffer);
				bytebuffer = new Byte[4];
				fs.Read (bytebuffer, 2, 2);
				retval.m_Version = BytesToUint (bytebuffer);
				bytebuffer = new Byte[4];
				fs.Read (bytebuffer, 0, 4);
				seconds = BytesToUint (bytebuffer);
				TimeSpan ts = new TimeSpan (0, (int)(seconds / 60), 0);
				retval.m_CreationDate = startdate + ts;
				fs.Read (bytebuffer, 0, 4);
				seconds = BytesToUint (bytebuffer);
				ts = new TimeSpan (0, (int)(seconds / 60), 0);
				retval.m_ModificationDate = startdate + ts;
				fs.Read (bytebuffer, 0, 4);
				seconds = BytesToUint (bytebuffer);
				ts = new TimeSpan (0, (int)(seconds / 60), 0);
				retval.m_LastBackupDate = startdate + ts;
				fs.Read (bytebuffer, 0, 4);
				retval.m_ModificationNumber = BytesToUint (bytebuffer);
				fs.Read (bytebuffer, 0, 4);
				retval.m_AppInfoID = BytesToUint (bytebuffer);
				fs.Read (bytebuffer, 0, 4);
				retval.m_SortInfoID = BytesToUint (bytebuffer);
				buffer = new char[4];
				sr.DiscardBufferedData ();
				sr.Read (buffer, 0, 4);
				retval.m_Type = new String (buffer);
				sr.Read (buffer, 0, 4);
				retval.m_Creator = new String (buffer);
				fs.Seek (68, SeekOrigin.Begin);
				fs.Read (bytebuffer, 0, 4);

				retval.m_UniqueIDSeed = BytesToUint (bytebuffer);
				fs.Read (bytebuffer, 0, 4);

				retval.m_NextRecordListID = BytesToUint (bytebuffer);
				bytebuffer = new Byte[4];
				fs.Read (bytebuffer, 2, 2);

				// Load RecordInfo

				retval.m_NumberOfRecords = BytesToUint (bytebuffer);
				retval.m_RecordInfoList = new PalmRecordInfo[retval.m_NumberOfRecords];
				retval.m_RecordList = new PalmRecord[retval.m_NumberOfRecords];
				for (int i = 0; i < retval.m_NumberOfRecords; i++) {

					fs.Read (bytebuffer, 0, 4);
					retval.m_RecordInfoList [i].DataOffset = BytesToUint (bytebuffer);

					bytebuffer = new Byte[4];
					fs.Read (bytebuffer, 3, 1);
					retval.m_RecordInfoList [i].Attributes = BytesToUint (bytebuffer);

					bytebuffer = new Byte[4];
					fs.Read (bytebuffer, 1, 3);
					retval.m_RecordInfoList [i].UniqueID = BytesToUint (bytebuffer);
				}

				//Load Records

				UInt32 StartOffset;
				UInt32 EndOffset;
				Int32 RecordLength;
				for (int i = 0; i < retval.m_NumberOfRecords - 1; i++) {
					StartOffset = retval.m_RecordInfoList [i].DataOffset;
					EndOffset = retval.m_RecordInfoList [i + 1].DataOffset;
					RecordLength = ((Int32)((long)(EndOffset - StartOffset)));
					fs.Seek (StartOffset, SeekOrigin.Begin);
					retval.m_RecordList [i].Data = new Byte[RecordLength];
					fs.Read (retval.m_RecordList [i].Data, 0, RecordLength);
				}

				StartOffset = retval.m_RecordInfoList [retval.m_NumberOfRecords - 1].DataOffset;
				RecordLength = (int)(fs.Length - ((Int32)((long)StartOffset)));
				fs.Seek (StartOffset, SeekOrigin.Begin);
				retval.m_RecordList [retval.m_NumberOfRecords - 1].Data = new Byte[RecordLength];
				fs.Read (retval.m_RecordList [retval.m_NumberOfRecords - 1].Data, 0, RecordLength);

				// LoadHeader
				List<Byte> empty2 = new List<Byte> ();
				List<Byte> temp = new List<Byte> ();
				empty2.Add (0);
				empty2.Add (0);

				List<Byte> headerdata = new List<Byte> ();

				headerdata.AddRange (retval.m_RecordList [0].Data);

				temp.AddRange (empty2);
				temp.AddRange (headerdata.GetRange (0, 2));
				retval.m_Compression = BytesToUint (temp.ToArray ());
				temp.Clear ();
				temp.AddRange (headerdata.GetRange (4, 4));
				retval.m_TextLength = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (empty2);
				temp.AddRange (headerdata.GetRange (8, 2));
				retval.m_TextRecordCount = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (empty2);
				temp.AddRange (headerdata.GetRange (10, 2));
				retval.m_TextRecordSize = BytesToUint (temp.ToArray ());

				temp.Clear ();
				temp.AddRange (headerdata.GetRange (12, 4));
				retval.m_CurrentPosition = BytesToUint (temp.ToArray ());
				;
			} finally {
				if (sr != null) {
					sr.Close ();
					sr.Dispose ();
				}
				if (fs != null) {
					fs.Close ();
					fs.Dispose ();
				}
			}
			return retval;
		}

		protected static UInt32 ReverseBytes (UInt32 value)
		{
			return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
			(value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
		}

		public static uint BytesToUint (byte[] bytes)
		{
			return (uint)((bytes [0] << 24) | (bytes [1] << 16) | (bytes [2] << 8) | bytes [3]);
		}

		public static uint BytesToUintR (byte[] bytes)
		{
			return ReverseBytes ((uint)((bytes [0] << 24) | (bytes [1] << 16) | (bytes [2] << 8) | bytes [3]));
		}
	}
}
