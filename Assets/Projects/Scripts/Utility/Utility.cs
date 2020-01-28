using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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
				var sleepTable = db.GetSleepTable ();
				return sleepTable.SelectAllOrderByAsc ().Select (data => {
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
			return DateTime.Parse (date);
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
    }
}
