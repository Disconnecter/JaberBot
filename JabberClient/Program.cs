using System;
using System.Collections.Generic;
using System.Text;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using agsXMPP.protocol.iq.roster;
using System.Threading;
using System.Net;
using System.Xml.Linq;
using System.Xml;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

//using Microsoft.SmartDevice.Connectivity;

namespace JabberClient
{
    class Globals
    {
        public static string JID_Sender;
        public static string Password;
        public static Jid jidSender;
        public static XmppClientConnection xmpp;
        public static string JID_Receiver;
        public static string DiskPath;
        public static string OnLinePath;
        public static string OwnExt;

        public static void init()
        {
            XDocument doc = XDocument.Load("options.xml");
            JID_Sender = doc.Root.Element("JID_Sender").Value;
            Password = doc.Root.Element("Password").Value;
            JID_Receiver = doc.Root.Element("JID_Receiver").Value;
            DiskPath = doc.Root.Element("DiskPath").Value;
            OnLinePath = doc.Root.Element("OnLinePath").Value;
            OwnExt = doc.Root.Element("OwnExt").Value;

            try
            {
                jidSender = new Jid(Globals.JID_Sender);
                xmpp = new XmppClientConnection(Globals.jidSender.Server);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    class Program
    {
        static bool _wait;
        static void Main(string[] args)
        {
            Globals.init();
            
            /** Starting Jabber Console, setting the Display settings**/
            Console.Title = "Jabber Client";
            Console.ForegroundColor = ConsoleColor.White;
            /** Login**/
            Console.WriteLine("Login");
            Console.WriteLine();
            Console.WriteLine("JID:{0} Pass *************", Globals.jidSender);
            
            /** Creating the Jid and the XmppClientConnection objects*/
            Jid jidSender = new Jid(Globals.JID_Sender);
            

            /** Open the connection
             * and register the OnLogin event handler*/
            try
            {
                Globals.xmpp.Open(jidSender.User, Globals.Password);
                Globals.xmpp.OnLogin += new ObjectHandler(xmpp_OnLogin);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            /** workaround, jus waiting till the login 
             * and authentication is finished**/
            Console.Write("Wait for Login ");
            int i = 0;
            _wait = true;
            do
            {
                Console.Write(".");
                i++;
                if (i == 10)
                    _wait = false;
                Thread.Sleep(250);
            } while (_wait);
            Console.WriteLine();

            /* just reading a few information*/
            Console.WriteLine("Login Status:");
            Console.WriteLine("xmpp Connection State {0}", Globals.xmpp.XmppConnectionState);
            Console.WriteLine("xmpp Authenticated? {0}", Globals.xmpp.Authenticated);
            Console.WriteLine();

            /*tell the world we are online and in chat mode*/
            Console.WriteLine("Sending Precence");
            Presence p = new Presence(ShowType.chat, "Online");
            p.Type = PresenceType.available;
            Globals.xmpp.Send(p);
            Console.WriteLine();

            /*get the roster (see who's online)*/
            Globals.xmpp.OnPresence += new PresenceHandler(xmpp_OnPresence);

            //wait until we received the list of available contacts            
            Console.WriteLine();
            Thread.Sleep(500);

            /* Chat starts here*/
            Console.WriteLine("Start Chat {0}", Globals.JID_Receiver);

            /* Catching incoming messages in
             * the MessageCallBack*/
            Globals.xmpp.MesagageGrabber.Add(new Jid(Globals.JID_Receiver),
                                     new BareJidComparer(),
                                     new MessageCB(MessageCallBack),
                                     null);

            /* Sending messages*/
            Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                                  MessageType.chat,
                                  "Hi Master"));
            string outMessage;
            bool halt = false;
            do
            {
                Console.ForegroundColor = ConsoleColor.Green;
                outMessage = Console.ReadLine();
                if (outMessage == "q!")
                {
                    halt = true;
                }
                else
                {
                    Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                                  MessageType.chat,
                                  outMessage));
                }

            } while (!halt);
            Console.ForegroundColor = ConsoleColor.White;

            /* finally we close the connection*/
            Globals.xmpp.Close();
        }

        // Is called, if the precence of a roster contact changed        
        static void xmpp_OnPresence(object sender, Presence pres)
        {
            Console.WriteLine("Available Contacts: ");
            Console.WriteLine("{0}@{1}  {2}", pres.From.User, pres.From.Server, pres.Type);
            Console.WriteLine();
        }

        // Is raised when login and authentication is finished 
        static void xmpp_OnLogin(object sender)
        {
            _wait = false;
            Console.WriteLine("Logged In");
        }

        //Handles incoming messages
        static void MessageCallBack(object sender,
                                    agsXMPP.protocol.client.Message msg,
                                    object data)
        {
            if (msg.Body != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string[] word = msg.Body.Split(' ');
                Console.WriteLine("{0}>> {1}", msg.From.User, msg.Body);

                switch (word[0])
                { 
                    case "!u":
                        {
                            Download(word[1]);
                            break;
                        }
                    case "!p":
                        {
                            Performance();
                            break;
                        }
                    case "!w":
                        {
                            Weather(word[1]);
                            break;
                        }
                    case "!si":
                        {
                            SysInf();
                            break;
                        }
                    default:
                        {
                            Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              "wrong command"));
                            break;
                        }
                }

                Console.ForegroundColor = ConsoleColor.Green;
            }
        }

        static void Performance()
        { 
            PerformanceCounter cpuCounter; 
            PerformanceCounter ramCounter; 

            cpuCounter = new PerformanceCounter(); 

            cpuCounter.CategoryName = "Processor"; 
            cpuCounter.CounterName = "% Processor Time"; 
            cpuCounter.InstanceName = "_Total"; 

            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                               MessageType.chat,
                               "CPU "+cpuCounter.NextValue() + "%"));
            Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                               MessageType.chat,
                               "RAM " + ramCounter.NextValue() + "Mb"));
        }

        static void Weather(String town)
        {
            try
            {
            String strReq = "http://www.google.com/ig/api?weather="+town;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(strReq);
            req.UserAgent = "Mozilla/5.0 Windows NT 6.1 AppleWebKit/536.5 KHTML, like Gecko Chrome/19.0.1084.46 Safari/536.5";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            StreamReader read = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding(1251));

            XmlTextReader r = null;
            
                r = new XmlTextReader(read);
                r.WhitespaceHandling = WhitespaceHandling.None;
                string temp = "Not Found";
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        if (r.Name == "current_conditions")
                        {
                            using (XmlReader rd = r.ReadSubtree())
                            {
                                while (rd.Read())
                                {
                                    if (rd.Name == "condition")
                                        temp = "Weather " + rd.GetAttribute("data") + "\n";
                                    if (rd.Name == "temp_c")
                                        temp += "Temp " + rd.GetAttribute("data") + "\n";
                                    if (rd.Name == "humidity")
                                        temp += rd.GetAttribute("data") + "\n";
                                    if (rd.Name == "wind_condition")
                                        temp += rd.GetAttribute("data") + "\n";   
                                }
                            }
                            break;
                        }
                    }
                }
                Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              temp));
            }
            catch (Exception e)
            {
                Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              e.Message));
            }
        }

        static void SysInf()
        {
            System.OperatingSystem OS = System.Environment.OSVersion;
            string os =  OS.VersionString;
            Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              "SysInf"));
        }

        static void Download(string url)
        {
            WebClient wc = new WebClient();
            try
            {
                System.Uri ui = new System.Uri(url);
                string path = Globals.DiskPath + ui.Segments[ui.Segments.Length - 1] + Globals.OwnExt;
                wc.DownloadFile(ui, path);
                string pathout = Globals.OnLinePath + ui.Segments[ui.Segments.Length - 1] + Globals.OwnExt;
                Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              pathout));

            }
            catch (Exception e)
            {
                Globals.xmpp.Send(new Message(new Jid(Globals.JID_Receiver),
                              MessageType.chat,
                              e.Message));
            }
        }
    }
}
