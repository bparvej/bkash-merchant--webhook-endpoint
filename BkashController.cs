using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Data.SqlClient;
using System.Configuration;

namespace AWSTest.Controllers
{
    public class BkashController : Controller
    {
        // GET: Bkash
        public ActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public string webhook(String id="")
        {
            string return_value = "";

            try
            {

                
                var jsonData = "";
                Stream req = Request.InputStream;
                req.Seek(0, System.IO.SeekOrigin.Begin);
                String json = new StreamReader(req).ReadToEnd();
                string mydate = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString()+ DateTime.Now.Year.ToString();
                write_to_file("raw_data" + mydate + ".txt", json);
                //write_to_file("test_file" + mydate + ".txt", "this is first test");
                if (json != null  && json !="")
                {
                    var sm = Amazon.SimpleNotificationService.Util.Message.ParseMessage(json);
                    if (sm.Type.Equals("SubscriptionConfirmation")) //for confirmation
                    {
                        //logger.Info("Received Confirm subscription request");
                        if (!string.IsNullOrEmpty(sm.SubscribeURL))
                        {
                            var uri = new Uri(sm.SubscribeURL);
                            //logger.Info("uri:" + uri.ToString());
                            var baseUrl = uri.GetLeftPart(System.UriPartial.Authority);
                            var resource = sm.SubscribeURL.Replace(baseUrl, "");
                            var response = new RestClient
                            {
                                BaseUrl = new Uri(baseUrl),
                            }.Execute(new RestRequest
                            {
                                Resource = resource,
                                Method = Method.GET,
                                RequestFormat = RestSharp.DataFormat.Xml
                            });
                        }
                    }
                    else // For processing of messages
                    {
                        //logger.Info("Message received from SNS:" + sm.TopicArn);
                        dynamic message = JsonConvert.DeserializeObject(sm.MessageText);

                        write_to_file("payload" + mydate + ".txt", sm.MessageText);
                        string date_time_converter = "cast(left('" + message.dateTime + "',8) as datetime) + cast(format(cast(right('" + message.dateTime + "', 6) as int),'##:##:##') as datetime)";
                        string qry = "INSERT INTO [T_BKASH_WEBHOOK1] ([number_from],[number_to],[date_time],[trx_id],[reference],[amount],[trx_type]) VALUES ('" + message.debitMSISDN + "', '" + message.creditShortCode + "', " + date_time_converter + ", '" + message.trxID + "', '" + message.transactionReference + "', CAST( '" + message.amount + "' AS NUMERIC(18,2)), '" + message.transactionType + "')";

                        SqlConnection oConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["RCLWEB"].ToString());
                        oConnection.Open();
                        SqlCommand oCOmmand = new SqlCommand(qry, oConnection);
                        oCOmmand.ExecuteNonQuery();

                        //logger.Info("EventTime : " + message.detail.eventTime);
                        //logger.Info("EventName : " + message.detail.eventName);
                        //logger.Info("RequestParams : " + message.detail.requestParameters);
                        //logger.Info("ResponseParams : " + message.detail.responseElements);
                        //logger.Info("RequestID : " + message.detail.requestID);
                    }



                    //do stuff


                    //write_to_file("first_file.txt", "parvej is working");

                }

                //write_to_file("first_file.txt", "parvej is working");
                return "Success";



               




            }
            catch (Exception ex)
            {
                
                string mydate = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString();
                write_to_file("error_log" + mydate + ".txt", ex.ToString());
                return ex.ToString();
            }

            return "Working good";


        }



        public string test()
        {
            try
            {
                //string path = Server.MapPath("/logs/"
                using (StreamReader file = new StreamReader(Server.MapPath("/logs/test.txt")))
                {
                    int counter = 0;
                    string ln;

                    while ((ln = file.ReadLine()) != null)
                    {
                        Console.WriteLine(ln);
                        counter++;

                        dynamic message = JsonConvert.DeserializeObject(ln);


                        //dynamic message = JsonConvert.DeserializeObject(sm.MessageText);

                        //write_to_file("payload" + mydate + ".txt", sm.MessageText);
                        string date_time_converter = "cast(left('" + message.dateTime + "',8) as datetime) + cast(format(cast(right('" + message.dateTime + "', 6) as int),'##:##:##') as datetime)";
                        string qry = "INSERT INTO [T_BKASH_WEBHOOK] ([number_from],[number_to],[date_time],[trx_id],[reference],[amount],[trx_type]) VALUES ('" + message.debitMSISDN + "', '" + message.creditShortCode + "', "+ date_time_converter + ", '" + message.trxID + "', '" + message.transactionReference + "', CAST( '" + message.amount + "' AS NUMERIC(18,2)), '" + message.transactionType + "')";

                        SqlConnection oConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["RCLWEB"].ToString());
                        oConnection.Open();
                        SqlCommand oCOmmand = new SqlCommand(qry, oConnection);
                        oCOmmand.ExecuteNonQuery();
                    }
                    file.Close();
                    return $"File has {counter} lines. \n" + ln;
                }

                return "success";

            }
            catch(Exception ex)
            {
                write_to_file("test_error.txt", ex.ToString());
                return "fail";
            }
        }

        



        ////function that is used by me
        ///


        private void write_to_file(string file_name, string file_content)
        {
            StreamWriter writer = new StreamWriter(Server.MapPath("/logs/"+file_name), true);
            writer.WriteLine(file_content);
            //return_value += "line 1";
            writer.Close();

        }

      

        ///end of my personal functions




    }
}