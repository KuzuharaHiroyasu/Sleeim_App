using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.UI;
using Kaimin.Managers;
using Asyncoroutine;

namespace Kaimin.Common
{
    /// <summary>
    /// ユーティリティ
    /// </summary>
    public class Utility
    {

        public enum Endian
        {
            Little, Big
        }

		/// <summary>
		/// 音声のパス
		/// </summary>
		public static string MusicTemplatePath () {
			return ApplicationPersistentPath ("Music/Template");
		}

        /// <summary>
        /// データディレクトリのパス
        /// </summary>
        /// <param name="plusPath"></param>
        /// <returns></returns>
        private static string ApplicationPersistentPath(string plusPath)
        {
            string path = "";
#if UNITY_EDITOR
            path = Application.dataPath + "/../";
#else
            path = Application.persistentDataPath + "/";
#endif
            path = path + plusPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        /// <summary>
        /// データディレクトリのパス
        /// </summary>
        /// <returns></returns>
        public static string GsDataPath()
        {
            return ApplicationPersistentPath("");
        }

        /// <summary>
        /// 指定のディレクトリ配下に含まれる指定拡張子の一覧を取得
        /// ※ディレクトリ数/ファイル数が多いと時間がかかるので注意
        /// </summary>
        /// <returns></returns>
        public static string[] GetAllFiles(string path, string extension)
        {
            var db = MyDatabase.Instance;

			if (db == null) {
				return null;
			} else {
                var sleepTable = db.GetSleepTable();
                return sleepTable.SelectDbSleepData().Select (data => {
					string dataPath = "";
					//pathの最後にスラッシュがあれば、取り除く
					path = ((path.Length - (path.LastIndexOf ('/') + 1)) == 0)
						? path.Substring (0, path.Length - 1)
						: path;
					dataPath += path;
					dataPath += "/";
					//filePathの先頭にスラッシュがあれば、取り除く
					string filePath = (data.file_path.IndexOf ('/') == 0)
						? data.file_path.Substring (1)
						: data.file_path;
					dataPath += filePath;
					return dataPath;
				}).ToArray ();
			}
        }

        /**
         * Get list of csv files that unread and unsaved to everage chart
         * Return Dictionary(fileId -> filePath)
         */
        public static Dictionary<int, string> getUnreadCsvFileList(int savedLastFileId)
        {
            Dictionary<int, string> unreadFileList = new Dictionary<int, string>();
            
            var db = MyDatabase.Instance;
            if (db != null) 
            { 
                var path = Kaimin.Common.Utility.GsDataPath();
                var sleepTable = db.GetSleepTable();
                var sleepData = sleepTable.SelectDbSleepData("WHERE file_id > " + savedLastFileId);

                foreach(var data in sleepData)
                {
                    string dataPath = "";
                    //pathの最後にスラッシュがあれば、取り除く
                    path = ((path.Length - (path.LastIndexOf('/') + 1)) == 0)
                        ? path.Substring(0, path.Length - 1)
                        : path;
                    dataPath += path;
                    dataPath += "/";
                    //filePathの先頭にスラッシュがあれば、取り除く
                    string filePath = (data.file_path.IndexOf('/') == 0)
                        ? data.file_path.Substring(1)
                        : data.file_path;
                    dataPath += filePath;

                    unreadFileList.Add(data.file_id, dataPath);
                }
            }

            return unreadFileList;
        }

        //睡眠データのファイルパスにある日付情報を解析して、DateTimeで返す
        public static DateTime TransFilePathToDate (string filePath) {
			//ファイル名のみ取り出す
			//スラッシュとバックスラッシュどちらが使われるか不安なため両方で試す '\\'はバックスラッシュのエスケープシーケンス
			int slashPos = filePath.LastIndexOf ('/') > filePath.LastIndexOf ('\\')
				? filePath.LastIndexOf ('/')
				: filePath.LastIndexOf ('\\');
			filePath = filePath.Substring (slashPos + 1);
			//.csvを取り除く
			//.が先頭から何文字目にあるか取得する
			int dotPos = filePath.LastIndexOf ('.');
			//.以降を削除する
			filePath = filePath.Remove (dotPos);
			//連続した時間の文字列をDateTimeに変換できるように区切る
			//元：YYYYMMDDHHMMSS → YYYY/MM/DD HH:MM/SS
			string YMD = filePath.Substring (0, 8);	//年月日
			YMD = YMD.Insert (4, "/");				//YYYY/MMDD Insertはカウント0はじまりで指定した位置の前に挿入っぽい
			YMD = YMD.Insert (7, "/");				//YYYY/MM/DD
			string HMS = filePath.Substring (8);	//時分秒
			HMS = HMS.Insert (2, ":");				//HH:MMSS
			HMS = HMS.Insert (5, ":");				//HH:MM:SS
			string date = YMD + " " + HMS;

            try
            {
                return DateTime.Parse(date);
            }
            catch (Exception e)
            {
                return DateTime.MinValue;
            }
		}

        /// <summary>
        /// 文字列を指定の文字数で分割
        /// </summary>
        /// <param name="str"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string[] SplitCountString(string str, int count)
        {
            var list = new List<string>();
            int length = (int)Math.Ceiling((double)str.Length / count);

            for (int i = 0; i < length; i++)
            {
                int start = count * i;
                if (str.Length <= start)
                {
                    break;
                }
                if (str.Length < start + count)
                {
                    list.Add(str.Substring(start));
                }
                else
                {
                    list.Add(str.Substring(start, count));
                }
            }

            return list.ToArray();
        }

        /// <summary>
        // 16進数文字列 => Byte配列
        /// </summary>
        public static byte[] StringToBytes(string str)
        {
            var bs = new List<byte>();
            for (int i = 0; i < str.Length / 2; i++)
            {
                bs.Add(Convert.ToByte(str.Substring(i * 2, 2), 16));
            }
            // "01-AB-EF" こういう"-"区切りを想定する場合は以下のようにする
            // var bs = str.Split('-').Select(hex => Convert.ToByte(hex, 16));
            return bs.ToArray();
        }

        /// <summary>
        /// エンディアンに従い適切なようにbyte[]を変換
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <param name="endian">エンディアン</param>
        /// <returns></returns>
        public static byte[] Reverse(byte[] bytes, Endian endian)
        {
            if (BitConverter.IsLittleEndian ^ endian == Endian.Little)
            {
                return bytes.Reverse().ToArray();
            }
            else
            {
                return bytes;
            }
        }

        //デバイスから取得したデータをリネームしてDBに登録する
        public static IEnumerator RegistDataToDB(List<string> dataPathList, List<string> dataNameList)
        {
            //DB登録
            string dataPath = Kaimin.Common.Utility.GsDataPath();
            for (int i = 0; i < dataPathList.Count; i++)
            {
                var sleepTable = MyDatabase.Instance.GetSleepTable();

                //仮のファイル名を指定されたファイル名に変更する
                var renamedFilePath = dataPathList[i];                                                  //例：112233445566/yyyyMM/tmp01.csv
                renamedFilePath = renamedFilePath.Substring(0, renamedFilePath.LastIndexOf('/') + 1);   //例：112233445566/yyyyMM/
                renamedFilePath = renamedFilePath + dataNameList[i];                                    //例：112233445566/yyyyMM/20180827092055.csv
                string fullOriginalFilePath = dataPath + dataPathList[i];
                string fullRenamedFilePath = dataPath + renamedFilePath;

                //ファイルが存在しているか確認する
                if (System.IO.File.Exists(fullOriginalFilePath))
                {
                    //リネーム後に名前が重複するデータがないか確認する
                    if (System.IO.File.Exists(fullRenamedFilePath))
                    {
                        //既に同じ名前のデータが存在した場合、元あったデータを削除する
                        System.IO.File.Delete(fullRenamedFilePath);
                    }
                    //ファイルを正常に処理できる事が確定したら
                    System.IO.File.Move(fullOriginalFilePath, fullRenamedFilePath); //リネーム処理
                }
                else
                {
                    Debug.Log(fullRenamedFilePath + " is not Exist...");
                }

                //データベースに変更後のファイルを登録する
                var dateString = dataNameList[i].Substring(0, dataNameList[i].LastIndexOf('.')); ////例：20180827092055
                Debug.Log("date:" + dateString + ", filePath:" + renamedFilePath);
                sleepTable.Update(new DbSleepData(dateString, renamedFilePath, false));
                Debug.Log("Insert Data to DB." + "path:" + renamedFilePath);
            }

            //DBに正しく保存できてるか確認用
            //var st = MyDatabase.Instance.GetSleepTable();
            //foreach (string path in st.SelectAllOrderByAsc().Select(data => data.file_path))
            //{
            //    Debug.Log("DB All FilePath:" + path);
            //}

            yield return null;
        }

        /**
         * filePath: /RD8001/Data/112233445566/yyyyMMdd/20180827092055_1.csvのようなファイルパス専用
         */
        public static String getDateFromDownloadCsvFilePath(string filePath)
        {
            string date = filePath.Substring(filePath.LastIndexOf('/') + 1); //例：20180827092055_1.csv
            date = date.Substring(0, date.LastIndexOf('.'));                 //例：20180827092055_1  
            date = date.Substring(0, date.LastIndexOf('_'));                 //例：20180827092055

            return date;
        }

        public static string getSecurityText()
        {
            string securityKey = "Sleeim_App_Android_iOS";
            string plainText = "Sleeim_Api";

            // Create sha256 hash
            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(securityKey));

            // Create secret IV
            byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };


            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;

            // Set key and IV
            byte[] aesKey = new byte[32];
            Array.Copy(key, 0, aesKey, 0, 32);
            encryptor.Key = aesKey;
            encryptor.IV = iv;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the 
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

            // Convert the plainText string into a byte array
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

            // Encrypt the input plaintext string
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);

            // Complete the encryption process
            cryptoStream.FlushFinalBlock();

            // Convert the encrypted data from a MemoryStream to a byte array
            byte[] cipherBytes = memoryStream.ToArray();

            // Close both the MemoryStream and the CryptoStream
            memoryStream.Close();
            cryptoStream.Close();

            // Convert the encrypted byte array to a base64 encoded string
            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

            // Return the encrypted data as a string
            return cipherText;
        }

        public static IEnumerator DeleteInvalidFile(String fullFilePath)
        {
            if (!File.Exists(fullFilePath))
            {
                yield break;
            }

            var sleepTable = MyDatabase.Instance.GetSleepTable();
            string fileName = Path.GetFileNameWithoutExtension(fullFilePath); //例:20191226231111

            //DBから削除する
            sleepTable.DeleteFromTable(SleepTable.COL_DATE, fileName);
            File.Delete(fullFilePath);

            if (HttpManager.IsInternetAvailable())
            {
                var sleepData = sleepTable.SelectFromColumn(SleepTable.COL_DATE, fileName);
                if (sleepData != null)
                {
                    string deviceId = HttpManager.getDeviceId(sleepData.file_path);
                    var deleteTask = HttpManager.DeleteFile(deviceId, fileName + "_" + sleepData.file_id + Path.GetExtension(fullFilePath));
                    yield return deleteTask.AsCoroutine();
                }
            }

            yield return null;
        }

        public static Color convertHexToColor(String htmlValue)
        {
            Color newCol;
            if (!ColorUtility.TryParseHtmlString(htmlValue, out newCol))
            {
                newCol = Color.blue;
            }

            return newCol;
        }

        /**
         * 条件１ 無呼吸平均回数(時)が５回以上
         * 条件２ いびき割合が50％以上
         * 条件３ いびき割合が25％以上
         * 条件４ 睡眠時間が７時間未満
         * レベル = 5 - 条件が当てはまっている数
         */
        public static int getSleepLevel(double apneaAverageCount, float pIbiki, double sleepTimeTotal)
        {
            int sleepLevel = 5;
            int countMatchCondition = 0;

            if (apneaAverageCount >= 5)
            {
                countMatchCondition++;
            }
            
            if (pIbiki >= 0.5)
            {
                countMatchCondition++;
            }
            
            if (pIbiki >= 0.25)
            {
                countMatchCondition++;
            }
            
            if (sleepTimeTotal < 7 * 3600)
            {
                countMatchCondition++;
            }

            return sleepLevel - countMatchCondition;
        }

        public static void makePieChartEmpty(PieChart pieChart)
        {
            //pieChart.circleOuter.SetActive(false);
            pieChart.circleOuter.GetComponent<Image>().color = Utility.convertHexToColor("#0063dc"); //Default is level 5
            pieChart.pieInfo.hidePieInfo();
            pieChart.piePrefab.fillAmount = 0;
            pieChart.sleepTimeText.text = "-";
            pieChart.sleepDateText.text = "";
        }

        public static void makePieChart(PieChart pieChart, double[] pieValues, string[] pieLabels, Color[] pieColors)
        {
            double total = 0f;
            int numPie = pieValues.Length;
            for (int i = 0; i < numPie; i++)
            {
                total += pieValues[i];
            }

            if (total > 0 && pieColors.Length >= numPie)
            {
                //Rotate image to top middle
                float zRotation = 180f;
                float zPieInfoRotation = 180f;
                float drawAngleTotal = 0;

                for (int i = 0; i < numPie; i++)
                {
                    pieChart.pieInfo.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zPieInfoRotation));
                    pieChart.pieInfo.hidePieInfo();

                    float fillAmount = (float)(pieValues[i] / total);
                    Image image = GameObject.Instantiate(pieChart.piePrefab) as Image;
                    image.transform.SetParent(pieChart.circleOuter.transform, false);
                    image.color = pieColors[i];
                    image.fillAmount = fillAmount;
                    image.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, zRotation));

                    int[] position = Utility.getPositionByPercent(fillAmount * 100, drawAngleTotal);
                    PieInfo p = GameObject.Instantiate(pieChart.pieInfo) as PieInfo;
                    p.transform.SetParent(image.transform, false);
                    p.transform.localPosition = new Vector3(position[0], position[1] + 189, 0);
                    if (pieValues[i] > 0)
                    {
                        p.drawPieInfo(pieLabels[i], pieValues[i]);
                    }

                    float rotateAngle = fillAmount * 360;
                    zPieInfoRotation += rotateAngle;
                    zRotation -= rotateAngle;
                    drawAngleTotal += rotateAngle;
                }
            }
        }

        public static int[] getPositionByPercent(double percent, float drawAngleTotal)
        {
            var dict = new Dictionary<double, int[]>();
            dict[0] = new int[2] { -2, -320 };
            dict[0.25] = new int[2] { -3, -320 };
            dict[0.5] = new int[2] { -4, -320 };
            dict[1] = new int[2] { -5, -320 };
            dict[1.5] = new int[2] { -7, -320 };
            dict[2] = new int[2] { -9, -320 };
            dict[2.5] = new int[2] { -10, -320 };
            dict[3] = new int[2] { -11, -320 };
            dict[4] = new int[2] { -15, -320 };

            if ((45 <= drawAngleTotal && drawAngleTotal <= 135) || (225 <= drawAngleTotal && drawAngleTotal <= 315))
            {
                dict[5] = new int[2] { -28, -325 };
                dict[6] = new int[2] { -35, -325 };
                dict[7] = new int[2] { -35, -325 };
                dict[7.5] = new int[2] { -35, -325 };
                dict[8] = new int[2] { -35, -325 };
                dict[9] = new int[2] { -35, -325 };
            }
            else
            {
                dict[5] = new int[2] { -19, -335 };
                dict[6] = new int[2] { -24, -335 };
                dict[7] = new int[2] { -28, -335 };
                dict[7.5] = new int[2] { -29, -335 };
                dict[8] = new int[2] { -32, -335 };
                dict[9] = new int[2] { -35, -335 };
            }

            dict[10] = new int[2] { -40, -320 };
            dict[11] = new int[2] { -43, -320 };
            dict[12] = new int[2] { -46, -320 };
            dict[13] = new int[2] { -50, -320 };
            dict[14] = new int[2] { -55, -315 };
            dict[15] = new int[2] { -60, -310 };
            dict[16] = new int[2] { -64, -310 };
            dict[17] = new int[2] { -68, -305 };
            dict[18] = new int[2] { -70, -300 };
            dict[19] = new int[2] { -72, -300 };

            dict[20] = new int[2] { -74, -295 };
            dict[21] = new int[2] { -77, -290 };
            dict[22] = new int[2] { -78, -288 };
            dict[23] = new int[2] { -80, -288 };
            dict[24] = new int[2] { -82, -286 };
            dict[25] = new int[2] { -86, -283 };
            dict[26] = new int[2] { -88, -278 };
            dict[27] = new int[2] { -89, -273 };
            dict[28] = new int[2] { -89, -271 };
            dict[29] = new int[2] { -91, -268 };

            dict[30] = new int[2] { -93, -265 };
            dict[31] = new int[2] { -95, -263 };
            dict[32] = new int[2] { -95, -257 };
            dict[33] = new int[2] { -97, -253 };
            dict[34] = new int[2] { -97, -251 };
            dict[35] = new int[2] { -97, -248 };
            dict[36] = new int[2] { -97, -245 };
            dict[37] = new int[2] { -99, -243 };
            dict[38] = new int[2] { -101, -241 };
            dict[39] = new int[2] { -101, -239 };

            dict[40] = new int[2] { -101, -236 };
            dict[41] = new int[2] { -103, -233 };
            dict[42] = new int[2] { -105, -230 };
            dict[43] = new int[2] { -107, -227 };
            dict[44] = new int[2] { -109, -224 };
            dict[45] = new int[2] { -109, -221 };
            dict[46] = new int[2] { -111, -218 };
            dict[47] = new int[2] { -112, -215 };
            dict[48] = new int[2] { -114, -212 };
            dict[49] = new int[2] { -115, -209 };

            dict[50] = new int[2] { -115, -205 };
            dict[51] = new int[2] { -118, -200 };
            dict[52] = new int[2] { -120, -196 };
            dict[53] = new int[2] { -122, -192 };
            dict[54] = new int[2] { -122, -188 };

            dict[55] = new int[2] { -122, -184 };
            dict[56] = new int[2] { -122, -180 };
            dict[57] = new int[2] { -122, -176 };
            dict[58] = new int[2] { -122, -172 };
            dict[59] = new int[2] { -122, -168 };

            dict[60] = new int[2] { -122, -164 };
            dict[61] = new int[2] { -122, -160 };
            dict[62] = new int[2] { -122, -156 };
            dict[63] = new int[2] { -122, -152 };
            dict[64] = new int[2] { -122, -148 };
            dict[65] = new int[2] { -122, -144 };
            dict[66] = new int[2] { -120, -140 };
            dict[67] = new int[2] { -118, -136 };
            dict[68] = new int[2] { -116, -132 };
            dict[69] = new int[2] { -112, -128 };

            dict[70] = new int[2] { -108, -124 };
            dict[71] = new int[2] { -106, -120 };
            dict[72] = new int[2] { -106, -116 };
            dict[73] = new int[2] { -106, -112 };
            dict[74] = new int[2] { -104, -108 };
            dict[75] = new int[2] { -100, -104 };
            dict[76] = new int[2] { -98, -100 };
            dict[77] = new int[2] { -96, -96 };
            dict[78] = new int[2] { -92, -92 };
            dict[79] = new int[2] { -88, -88 };

            dict[80] = new int[2] { -84, -84 };
            dict[81] = new int[2] { -80, -80 };
            dict[82] = new int[2] { -76, -76 };
            dict[83] = new int[2] { -74, -74 };
            dict[84] = new int[2] { -72, -72 };
            dict[85] = new int[2] { -70, -70 };
            dict[86] = new int[2] { -68, -70 };
            dict[87] = new int[2] { -66, -69 };
            dict[88] = new int[2] { -64, -69 };
            dict[89] = new int[2] { -61, -69 };

            dict[90] = new int[2] { -58, -69 };
            dict[91] = new int[2] { -54, -69 };
            dict[92] = new int[2] { -50, -69 };
            dict[93] = new int[2] { -46, -69 };
            dict[94] = new int[2] { -42, -69 };
            dict[95] = new int[2] { -38, -69 };
            dict[96] = new int[2] { -32, -69 };
            dict[97] = new int[2] { -26, -69 };
            dict[98] = new int[2] { -21, -69 };
            dict[99] = new int[2] { -11, -69 };
            dict[99.5] = new int[2] { -8, -69 };
            dict[100] = new int[2] { -0, -69 };

            //key1 <= percent <= key2
            double key1 = 0;
            double key2 = 0;
            foreach (KeyValuePair<double, int[]> entry in dict)
            {
                if (percent <= entry.Key)
                {
                    key2 = entry.Key;
                    break;
                }
                else
                {
                    key1 = entry.Key;
                }
            }

            return (key2 - percent > percent - key1) ? dict[key1] : dict[key2];
        }
    }
}
