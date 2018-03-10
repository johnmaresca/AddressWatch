/*
 * Copyright (c) 2018 John Maresca

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Configuration;
namespace AddressWatch
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                while (true)
                {
                    //data xml file
                    Data data = null;
                    if (!File.Exists(ConfigurationManager.AppSettings["DataFile"]))
                    {
                        data = new Data() { FinalBalance = "0", LastUpdated = DateTime.Now };
                        Serialize(data);
                    }
                    else
                        data = Deserialize(ConfigurationManager.AppSettings["DataFile"]);

                    //get json
                    string jsonAddress = "https://blockchain.info/rawaddr/" + ConfigurationManager.AppSettings["Address"];
                    WebRequest req = WebRequest.Create(jsonAddress);
                    WebResponse resp = req.GetResponse();
                    Stream recStream = resp.GetResponseStream();
                    StreamReader jsonReader = new StreamReader(recStream);
                    String json = jsonReader.ReadToEnd();
                    resp.Close();

                    dynamic bitcoin = JsonConvert.DeserializeObject(json);
                    string newBalance = (string)bitcoin.final_balance;
                    if (newBalance != data.FinalBalance)
                    {
                       
                        //serialize new balance
                        data.FinalBalance = newBalance;
                        data.LastUpdated = DateTime.Now;
                        Serialize(data);

                        //balance changed! send email/sms
                        SendEmail("Watched Balance Changed!", "New Balance: " + newBalance, ConfigurationManager.AppSettings["ToAddress"]);

                    }
                    //wait for 30 seconds
                    Thread.Sleep(30000);
                }
            }catch(Exception ex)
            {
                Console.Write("Error: " + ex.Message);
                throw ex;
            }
        }

        static void Serialize(Data data)
        {
            XmlSerializer writer = new XmlSerializer(typeof(Data));
            FileStream file = System.IO.File.Create(ConfigurationManager.AppSettings["DataFile"]);
            writer.Serialize(file, data);
            file.Close();
        }

        static Data Deserialize(string fileName)
        {
            XmlSerializer reader = new XmlSerializer(typeof(Data));
            StreamReader file = new StreamReader(ConfigurationManager.AppSettings["DataFile"]);
            return (Data)reader.Deserialize(file);
        }

        static void SendEmail(string subject, string message, string toAddress)
        {
            SmtpClient client = new SmtpClient(ConfigurationManager.AppSettings["SMTPClientAddress"], 587);
            client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTPUserName"], ConfigurationManager.AppSettings["SMTPPassword"]);
            client.EnableSsl = true;
            MailMessage mailMessage = new MailMessage(ConfigurationManager.AppSettings["FromAddress"], ConfigurationManager.AppSettings["ToAddress"]);
            mailMessage.Body = message;
            mailMessage.Subject = subject;
            client.Send(mailMessage);
        }
    }
}
