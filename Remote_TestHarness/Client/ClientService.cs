/////////////////////////////////////////////////////////////////////
// ClientService.cs - Service class used by remote channels to     //
//                    create proxy objects and communicate with    // 
//                    client                                       //
//  ver 1.0                                                        //
//  Language:      Visual C#  2015                                 //
//  Platform:      Mac, Windows 7                                  //
//  Application:   TestHarness - Project4                          //
//                 CSE681 - Software Modeling and Analysis,        //
//                 Fall 2016                                       //
//  Author:        saisumanth, Syracuse University                 //
//                 (315) 828-6589, sgopiset@syr.edu                //
//                                                                 //
//  Source:        Jim Fawcett                                     //
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                    IncludeExceptionDetailInFaults = true)]
    class ClientService : IService
    {
        static string ToSendPath = "..\\..\\FilesToSend";
        static string SavePath = "..\\..\\FilesReceived";
        int BlockSize = 1024;
        byte[] block = null;

        public ClientService()
        {
            block = new byte[BlockSize];
        }
        public void PostMessage(Message msg)
        {
            Client.EnqueueMessages(msg);
        }

        public Stream downLoadFile(string filename)
        {
           string sfilename = Path.Combine(ToSendPath, filename);
           FileStream outStream = null;
           if (File.Exists(sfilename))
           {
               outStream = new FileStream(sfilename, FileMode.Open);
           }
           else
               throw new Exception("open failed for \"" + filename + "\"");
            Console.Write("\n  Sent \"{0}\" .", filename);
            return outStream;
        }

        public void upLoadFile(FileTransferMessage msg)
        {
            int totalBytes = 0;
            string filename = msg.filename;
            string rfilename = Path.Combine(SavePath, filename);
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
            using (var outputStream = new FileStream(rfilename, FileMode.Create))
            {
                while (true)
                {
                    int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
                    totalBytes += bytesRead;
                    if (bytesRead > 0)
                        outputStream.Write(block, 0, bytesRead);
                    else
                        break;
                }
            }
            Console.Write(
              "\n  Received file \"{0}\" of {1} bytes .",
              filename, totalBytes);
        }
    }
}
