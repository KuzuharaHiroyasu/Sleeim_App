using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class DbSleepData : AbstractData {
	public string date = "";
	public string file_path = "";
	public bool send_flag = false;
	public int file_id = 0;

	public DbSleepData (string date, string filePath, bool sendFlag) : base () {
		this.date = date;
		this.file_path = filePath;
		this.send_flag = sendFlag;
	}

    public DbSleepData(string date, string filePath, bool sendFlag, int fileId) : base()
    {
        this.date = date;
        this.file_path = filePath;
        this.send_flag = sendFlag;
        this.file_id = fileId;
    }

    public override void DebugPrint () {
		Debug.Log ("date = " + date + ", filePath = " + file_path + ", sendFlag = " + send_flag);
	}
}

public class SleepTable : AbstractDbTable<DbSleepData> {

	private static readonly string COL_DATE = "date";
	private static readonly string COL_FILEPATH = "file_path";
	private static readonly string COL_SENDFLAG = "send_flag";
	private static readonly string COL_FILE_ID = "file_id";

	public SleepTable (ref SqliteDatabase db) : base (ref db) {
	}

	protected override string TableName {
		get {
			return "SleepTable";
		}
	}

	protected override string PrimaryKeyName {
		get {
			return "date";
		}
	}

	public override void MargeData (ref SqliteDatabase oldDb) {
		SleepTable oldTable = new SleepTable (ref oldDb);
		foreach (DbSleepData oldData in oldTable.SelectAll ()) {
			Update (oldData);
		}
	}

	public override void Update(DbSleepData data) {
		StringBuilder query = new StringBuilder ();
		DbSleepData selectData = SelectFromPrimaryKey (long.Parse (data.date));
		if (selectData == null) {
			query.Append ("INSERT INTO ");
			query.Append (TableName);
			query.Append (" VALUES(");
			query.Append ("'");
			query.Append (data.date);
			query.Append ("'");
			query.Append (",");
			query.Append ("'");
			query.Append (data.file_path);
			query.Append ("'");
			query.Append (",");
			query.Append ("'");
			query.Append (data.send_flag ? DbDefine.DB_VALUE_TRUE.ToString () : DbDefine.DB_VALUE_FALSE.ToString ());
			query.Append ("'");
			query.Append (");");
		} else {
			query.Append ("UPDATE ");
			query.Append (TableName);
			query.Append (" SET ");
			query.Append (COL_DATE);
			query.Append ("=");
			query.Append ("'");
			query.Append (data.date);
			query.Append ("'");
			query.Append (",");
			query.Append (COL_FILEPATH);
			query.Append ("=");
			query.Append ("'");
			query.Append (data.file_path);
			query.Append ("'");
			query.Append (",");
			query.Append (COL_SENDFLAG);
			query.Append ("=");
			query.Append ("'");
			query.Append (data.send_flag ? DbDefine.DB_VALUE_TRUE.ToString () : DbDefine.DB_VALUE_FALSE.ToString ());
			query.Append ("'");
			query.Append (" WHERE ");
			query.Append (COL_DATE);
			query.Append ("=");
			query.Append ("'");
			query.Append (data.date);
			query.Append ("'");
			query.Append (";");
		}
		mDb.ExecuteNonQuery (query.ToString ());
	}

	/// <summary>
	/// 主キーを指定して該当するデータを削除する
	/// </summary>
	/// <param name="date">主キー</param>
	public void DeleteFromPrimaryKey (long date) {
		StringBuilder query = new StringBuilder ();
		query.Append ("DELETE FROM ");
		query.Append (TableName);
		query.Append (" WHERE ");
		query.Append (PrimaryKeyName);
		query.Append ("=");
		query.Append (date.ToString ());
		query.Append (";");
		mDb.ExecuteNonQuery (query.ToString ());
	}

	protected override DbSleepData PutData (DataRow row) {
		DbSleepData data = new DbSleepData (GetStringValue (row, COL_DATE), GetStringValue (row, COL_FILEPATH), GetBoolValue (row, COL_SENDFLAG));
		return data;
	}

    protected DbSleepData PutDataWithFileId(DataRow row)
    {
        DbSleepData data = new DbSleepData(GetStringValue(row, COL_DATE), GetStringValue(row, COL_FILEPATH), GetBoolValue(row, COL_SENDFLAG), GetIntValue(row, COL_FILE_ID));
        return data;
    }

    public List<DbSleepData> SelectDbSleepData(string whereCondition = "")
    {
        List<DbSleepData> dataList = new List<DbSleepData>();
        StringBuilder query = new StringBuilder();
        query.Append("SELECT * FROM ");
        query.Append(TableName);
        query.Append(" " + whereCondition + " ");
        query.Append(" ORDER BY file_id ASC, ");
        query.Append(PrimaryKeyName);
        query.Append(" ASC");
        query.Append(";");
        DataTable dt = mDb.ExecuteQuery(query.ToString());
        foreach (DataRow row in dt.Rows)
        {
            dataList.Add(PutDataWithFileId(row));
        }

        return dataList;
    }
}
