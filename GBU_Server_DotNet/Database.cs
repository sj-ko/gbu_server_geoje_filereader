﻿//#define __USE_FIREBIRD__ // gaenari firebird db
#define __USE_AWS_S3_UPLOAD__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !__USE_FIREBIRD__
using MySql.Data;
using MySql.Data.MySqlClient;
#endif
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.IO;
#if __USE_FIREBIRD__
using FirebirdSql;
using FirebirdSql.Data;
using FirebirdSql.Data.FirebirdClient;
#endif
#if __USE_AWS_S3_UPLOAD__
using Amazon.S3;
using Amazon.S3.Transfer;
#endif

namespace GBU_Server_DotNet
{
    public class Database
    {
        private string _savepath = "";
        public string SavePath
        {
            get
            {
                return _savepath;
            }
            set
            {
                _savepath = value;
            }
        }

#if __USE_FIREBIRD__
        private string strConn = "User=sysdba;" +
                                "Password=masterkey;" +
                                "Database=D:/G/DATA/OPOSDB;" +
                                "Server=localhost;" +
                                "Port=3050;";
        private FbConnection conn;
#else
        private string strConn = "Server=gbuanpr.c6jnuct8qbno.ap-northeast-2.rds.amazonaws.com;Database=gbuanpr;Uid=ksj828;Pwd=gbudata1234;";
        private MySqlConnection conn;
#endif

        public Database()
        {
#if __USE_FIREBIRD__
            conn = new FbConnection(strConn);
#else
            conn = new MySqlConnection(strConn);
#endif
        }

        public int InsertPlate(int camid, DateTime datetime, string plate, Image image)
        {
            try
            {
                conn.Open();
#if __USE_FIREBIRD__
                string fbYMDHNS = string.Format("{0:yyMMddHHmmss}", datetime);
                string fbCARNO = plate;
                string fbOK = "";
                string fbCID = Convert.ToString(camid, 10);

                String sql = "INSERT INTO TCARL (CARL_YMDHNS, CARL_CARNO, CARL_OK, CARL_CID) " + "VALUES (@fbYMDHNS, @fbCARNO, @fbOK, @fbCID)";
#else
                String sql = "INSERT INTO anpr_test1 (camId, dateTime, plate, image) " + "VALUES (@camid, @datetime, @plate, @image)";
#endif

#if __USE_FIREBIRD__
                FbCommand cmd = new FbCommand(sql, conn);
#else
                MySqlCommand cmd = new MySqlCommand(sql, conn);
#endif
                cmd.Connection = conn;
                cmd.CommandText = sql;
#if __USE_FIREBIRD__
                //cmd.Parameters.Add("@id", MySqlDbType.Int32, 4);
                cmd.Parameters.Add("@fbYMDHNS", FbDbType.VarChar, 12);
                cmd.Parameters.Add("@fbCARNO", FbDbType.Text);
                cmd.Parameters.Add("@fbOK", FbDbType.Text);
                cmd.Parameters.Add("@fbCID", FbDbType.VarChar, 2);

                //cmd.Parameters[0].Value = id;
                cmd.Parameters[0].Value = fbYMDHNS;
                cmd.Parameters[1].Value = fbCARNO;
                cmd.Parameters[2].Value = fbOK;
                cmd.Parameters[3].Value = fbCID;
#else
                //cmd.Parameters.Add("@id", MySqlDbType.Int32, 4);
                cmd.Parameters.Add("@camid", MySqlDbType.Int32, 4);
                cmd.Parameters.Add("@datetime", MySqlDbType.DateTime);
                cmd.Parameters.Add("@plate", MySqlDbType.VarChar, 32);
                cmd.Parameters.Add("@image", MySqlDbType.MediumBlob);

                //cmd.Parameters[0].Value = id;
                cmd.Parameters[0].Value = camid;
                cmd.Parameters[1].Value = datetime;
                cmd.Parameters[2].Value = plate;
                cmd.Parameters[3].Value = image;
#endif



                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "::" + e.StackTrace);
                conn.Close();
            }

#if __USE_AWS_S3_UPLOAD__
            try
            {
                string existingBucketName = "gbustorage-1";     // gbustorage-1
                //string keyName            = "*** Provide your object key ***"; // 객체 이름 (임의 지정)
                //string filePath           = "*** Provide file name ***";       // 파일 이름

                TransferUtility transferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.APNortheast2));

                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Jpeg);
                    transferUtility.Upload(ms, existingBucketName, camid.ToString() + "_" + datetime.ToString() + "_" + plate + ".jpg");
                }
            }
            catch (AmazonS3Exception s3Exception)
            {
                Console.WriteLine(s3Exception.Message,
                                  s3Exception.InnerException);
            }
#endif

            return 0;
        }

        public int DeletePlate(string name, string id, string pwd, string url)
        {
            return 0;
        }

        public int UpdatePlate(int no)
        {
            return 0;
        }

        public int SearchPlate(string str, ref DataTable resultTable)
        {
            try
            {
                conn.Open();
                DataSet ds = new DataSet();
#if __USE_FIREBIRD__
                FbDataAdapter da = new FbDataAdapter("SELECT * FROM TCARL WHERE CARL_CARNO like '%" + str + "%'", conn);
#else
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM anpr_test1 WHERE plate like '%" + str + "%'", conn);
#endif
                da.Fill(ds, "mytable");

                DataTable dt = ds.Tables["mytable"];
                foreach (DataRow dr in dt.Rows)
                {
#if __USE_FIREBIRD__
                    Console.WriteLine(string.Format("Name = {0}, Desc = {1}", dr["CARL_YMDHNS"], dr["CARL_CARNO"]));
#else
                    Console.WriteLine(string.Format("Name = {0}, Desc = {1}", dr["dateTime"], dr["plate"]));
#endif
                }
                resultTable = dt;
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "::" + e.StackTrace);
                conn.Close();
            }
            return 0;
        }

        public int SearchPlateForFile(int ch, string str, ref DataTable resultTable)
        {
            DataTable dt = new DataTable("mytable");
            dt.Columns.Add("camId");
            dt.Columns.Add("dateTime");
            dt.Columns.Add("plate");
            dt.Columns.Add("imageFilePath");

            for (int i = 0; i < 20; i++)
            {
                if (ch != -1)
                {
                    i = ch;
                }

                string path = _savepath + "\\ch" + i;
                if (File.Exists(path + "\\anprresult.txt"))
                {
                    string[] lines = System.IO.File.ReadAllLines(path + "\\anprresult.txt");
                    foreach (string line in lines)
                    {
                        string[] values = line.Split(',');
                        if (values[1].Contains(str))
                        {
                            DataRow row = dt.NewRow();
                            row[0] = i;
                            row[1] = values[2]; // datetime
                            row[2] = values[1]; // plate
                            row[3] = values[3]; // imagepath
                            dt.Rows.Add(row);
                        }
                    }
                }

                if (ch != -1)
                {
                    break;
                }
            }

            resultTable = dt;

            return 0;
        }

        public void InsertPlateText(int camid, DateTime datetime, string plate, Image image)
        {
            string path = _savepath + "\\ch" + camid;
            string logFileName = "\\anprresult.txt";
            string dtStr = String.Format("{0:yyyyMMdd_HHmmss}", datetime);
            string imageFileName = "\\Camera" + camid + "_" + plate + "_" + dtStr + ".jpg";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (!File.Exists(path + logFileName))
                File.Create(path + logFileName).Close();

            try
            {
                using (Bitmap tempImage = new Bitmap(image))
                {
                    tempImage.Save(path + imageFileName, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured creating image :: " + ex.Message);
            }

            StreamWriter file = new StreamWriter(path + logFileName, true);
            file.WriteLine(camid + "," + plate + "," + datetime + "," + path + imageFileName);
            file.Flush();
            file.Close();
        }

        public void InsertPlateXML(int camid, DateTime datetime, string plate, Image image)
        {
            // to be added
            //

        }


    }
}
