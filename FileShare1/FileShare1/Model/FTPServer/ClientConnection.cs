using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileShare1.Model.FTPServer
{
    class ClientConnection
    {
        private enum DataConnectionType
        {
            Passive,
            Active
        }

        private TcpClient controlClient;
        private TcpClient dataClient;

        private NetworkStream controlStream;
        private StreamReader controlReader;
        private StreamWriter controlWriter;
        private StreamReader dataReader;
        private StreamWriter dataWriter;

        private string username;
        private string transferType;
        private DataConnectionType dataConnectionType = DataConnectionType.Active;
        private IPEndPoint dataEndpoint;
        private TcpListener passiveListener;
        private string currentDirectory;
        private string root = "/storage/emulated/0";
        private string renameFrom;

        public ClientConnection(TcpClient client)
        {
            controlClient = client;
            controlStream = controlClient.GetStream();
            controlReader = new StreamReader(controlStream);
            controlWriter = new StreamWriter(controlStream);
        }

        public void HandleClient(object obj)
        {
            controlWriter.WriteLine("220 Service ready.");
            controlWriter.Flush();

            string line;

            try
            {
                while(!string.IsNullOrWhiteSpace(line = controlReader.ReadLine()))
                {
                    string response = null;
                    string[] command = line.Split(' ');
                    string cmd = command[0].ToUpperInvariant();
                    string arguments = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;

                    if (string.IsNullOrWhiteSpace(arguments))
                        arguments = null;

                    if (cmd != "RNTO")
                        renameFrom = null;

                    if (response == null)
                    {
                        switch(cmd)
                        {
                            case "USER":
                                response = User(arguments);
                                break;
                            case "PASS":
                                response = Password(arguments);
                                break;
                            case "CWD":
                                response = ChangeWorkingDirectory(arguments);
                                break;
                            case "CDUP":
                                response = ChangeWorkingDirectory("..");
                                break;
                            case "PWD":
                                response = PrintWorkingDirectory();
                                break;
                            case "TYPE":
                                string[] splitArgs = arguments.Split(' ');
                                response = Type(splitArgs[0], splitArgs.Length > 1 ? splitArgs[1] : null);
                                break;
                            case "PORT":
                                response = Port(arguments);
                                break;
                            case "PASV":
                                response = Passive();
                                break;
                            case "LIST":
                                response = List(arguments);
                                break;
                            case "OPTS":
                                response = "200 OK";
                                break;
                            case "RETR":
                                response = Retrieve(arguments);
                                break;
                            case "STOR":
                                response = Store(arguments);
                                break;
                            case "SYST":
                                response = "215 System";
                                break;
                            case "NOOP":
                                response = "200 OK";
                                break;
                            case "SIZE":
                                response = FileSize(arguments);
                                break;
                            case "DELE":
                                response = Delete(arguments);
                                break;
                            case "RMD":
                                response = RemoveDir(arguments);
                                break;
                            case "MKD":
                                response = MakeDir(arguments);
                                break;
                            case "RNFR":
                                renameFrom = arguments;
                                response = "350 Waiting for RNTO command.";
                                break;
                            case "RNTO":
                                response = Rename(renameFrom, arguments);
                                break;
                            case "QUIT":
                                response = "221 Service closing control connection.";
                                break;


                            default:
                                response = "502 Command not implemented.";
                                break;                        
                        }
                    }

                    if (!controlClient.Connected || controlClient == null)
                    {
                        break;
                    }
                    else
                    {
                        
                        controlWriter.WriteLine(response);
                        controlWriter.Flush();

                        if (response.StartsWith("221"))
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                //throw;
                return;
            }
        }

        private bool IsPathValid(string path)
        {
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeFileName(string path)
        {
            if (path == null)
                path = string.Empty;

            if (path == "/")
                return root;
            else if (path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                path = new FileInfo(Path.Combine(root, path.Substring(1))).FullName;
            else
                path = new FileInfo(Path.Combine(currentDirectory, path)).FullName;

            return IsPathValid(path) ? path : null;
        }

        #region FTP Commands

        private string User(string username)
        {
            this.username = username;
            return "331 Username OK, waiting for password.";
        }

        private string Password(string password)
        {
            currentDirectory = root;
            return "230 User logged in";
            //return "530 Not logged in";
        }

        private string ChangeWorkingDirectory(string path)
        {
            if (path == "/")
            {
                currentDirectory = root;
            }
            else
            {
                if (path.StartsWith("/"))
                {
                    path = Path.Combine(root, path.Substring(1));
                }
                else
                {
                    path = Path.Combine(currentDirectory, path);
                }
                
                if (Directory.Exists(path))
                {
                    currentDirectory = new DirectoryInfo(path).FullName;
                }
                else
                {
                    currentDirectory = root;
                }
            }

            return "250 Changed to new directory";

        }

        private string Type(string typeCode, string formatControl)
        {
            string response = "";

            switch (typeCode)
            {
                case "A":
                case "I":
                    response = "200 OK";
                    transferType = typeCode;
                    break;
                case "E":
                case "L":

                default:
                    response = "504 Command not implemented for this parameter.";
                    break;
            }

            if (formatControl != null)
            {
                switch (formatControl)
                {
                    case "N":
                        response = "200 OK";
                        break;
                    case "T":
                    case "C":

                    default:
                        response = "504 Command not implemented for this parameter.";
                        break;
                }
            }

            return response;
        }

        private string Port(string hostPort)
        {
            dataConnectionType = DataConnectionType.Active;

            string[] ipAndPort = hostPort.Split(',');
            byte[] ipAdress = ipAndPort.Take(4).Select(s => Convert.ToByte(s)).ToArray();
            byte[] port = ipAndPort.Skip(4).Select(s => Convert.ToByte(s)).ToArray();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(port);

            dataEndpoint = new IPEndPoint(new IPAddress(ipAdress), BitConverter.ToInt16(port, 0));

            return "200 OK";
        }

        private string Passive()
        {
            dataConnectionType = DataConnectionType.Passive;

            IPAddress localIP = ((IPEndPoint)controlClient.Client.LocalEndPoint).Address;

            passiveListener = new TcpListener(localIP, 0);
            passiveListener.Start();

            IPEndPoint localEndPoint = ((IPEndPoint)passiveListener.LocalEndpoint);

            byte[] address = localEndPoint.Address.GetAddressBytes();
            short port = (short)localEndPoint.Port;
            byte[] portArray = BitConverter.GetBytes(port);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            return string.Format("227 Entering passive mode ({0},{1},{2},{3},{4},{5})", address[0], address[1], address[2], address[3], portArray[0], portArray[1]);
        }

        private string List(string path)
        {
            if (path == null)
            {
                path = string.Empty;
            }

            path = new DirectoryInfo(Path.Combine(currentDirectory, path)).FullName;

            if(IsPathValid(path))
            {
                if(dataConnectionType == DataConnectionType.Active)
                {
                    dataClient = new TcpClient();
                    dataClient.BeginConnect(dataEndpoint.Address, dataEndpoint.Port, DoList, path);
                }
                else
                {
                    passiveListener.BeginAcceptTcpClient(DoList, path);
                }

                return string.Format("150 Opening {0} mode data transfer for LIST", dataConnectionType);
            }

            return "450 Requested file action not taken";
        }

        private void DoList(IAsyncResult result)
        {
            if(dataConnectionType == DataConnectionType.Active)
            {
                dataClient.EndConnect(result);
            }
            else
            {
               dataClient = passiveListener.EndAcceptTcpClient(result);
            }

            string path = (string)result.AsyncState;

            using (NetworkStream dataStream = dataClient.GetStream())
            {
                dataReader = new StreamReader(dataStream);
                dataWriter = new StreamWriter(dataStream);

                IEnumerable<string> directories = Directory.EnumerateDirectories(path);
                
                foreach(string dir in directories)
                {
                    DirectoryInfo d = new DirectoryInfo(dir);

                    //string date = d.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ? d.LastWriteTime.ToString("MMM dd yyyy") : d.LastWriteTime.ToString("MMM dd HH:mm");
                    string date = d.LastWriteTime.ToString("MM-dd-yy HH:mm");
                    //string line = string.Format("drwxr-xr--x    2 2003     2003     {0,8} {1} {2}", "4096", date, d.Name);
                    string line = string.Format("{0} <DIR> {1}", date, d.Name);

                    dataWriter.WriteLine(line);
                    
                }
                /*
                string line1 = "04-04-03 10:30PM 325 PrettyFly.txt";
                dataWriter.WriteLine(line1);
                dataWriter.Flush();
                */
                IEnumerable<string> files = Directory.EnumerateFiles(path);

                foreach(string file in files)
                {
                    FileInfo f = new FileInfo(file);

                    //string date = f.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ? f.LastWriteTime.ToString("MMM dd yyyy") : f.LastWriteTime.ToString("MMM dd HH:mm");
                    string date = f.LastWriteTime.ToString("MM-dd-yy HH:mm");
                    //string line = string.Format("-rw-r--r--    2 2003     2003     {0,8} {1}", f.Length, f.Name);
                    string line = string.Format("{0} {1} {2}", date, f.Length, f.Name);
                    dataWriter.WriteLine(line);
                    //dataWriter.Flush();
                }
                dataWriter.Flush();
            }

            dataClient.Close();
            dataClient = null;

            controlWriter.WriteLine("226 Transfer complete.");
            controlWriter.Flush();
        }

        private string Retrieve(string path)
        {
            path = NormalizeFileName(path);

            if (IsPathValid(path))
            {
                if(File.Exists(path))
                {
                    if (dataConnectionType == DataConnectionType.Active)
                    {
                        dataClient = new TcpClient();
                        dataClient.BeginConnect(dataEndpoint.Address, dataEndpoint.Port, DoRetrieve, path);
                    }
                    else
                    {
                        passiveListener.BeginAcceptTcpClient(DoRetrieve, path);
                    }

                    return string.Format("150 Opening {0} mode data transfer.", dataConnectionType);
                }
            }

            return "550 File not found.";
        }

        private void DoRetrieve(IAsyncResult result)
        {
            if(dataConnectionType == DataConnectionType.Active)
            {
                dataClient.EndConnect(result);
            }
            else
            {
                dataClient = passiveListener.EndAcceptTcpClient(result);
            }

            string path = (string)result.AsyncState;

            try
            {
                using (NetworkStream dataStream = dataClient.GetStream())
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    CopyStream(fs, dataStream);
                }

            }
            catch
            {
                dataClient.Close();
                dataClient = null;

                controlWriter.WriteLine("426 Closing data connection, file transfer aborted.");
                controlWriter.Flush();
                return;
            }



            dataClient.Close();
            dataClient = null;

            controlWriter.WriteLine("226 Closing data connection, file tranfer succesful");
            controlWriter.Flush();
            
        }

        private static void CopyStreamImage(Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            int total = 0;
           
            while ((count = input.Read(buffer, 0, bufferSize)) > 0)
            {
                
                output.Write(buffer, 0, count);
                total += count;
            }
            
        }

        private static void CopyStramAscii(Stream input, Stream output, int bufferSize)
        {
            char[] buffer = new char[bufferSize];
            int count = 0;
            int total = 0;

            using (StreamReader reader = new StreamReader(input))
            using (StreamWriter writer = new StreamWriter(output, Encoding.ASCII))
            {
                while ((count = reader.Read(buffer, 0, bufferSize)) > 0)
                {
                    writer.Write(buffer, 0 , count);
                    total += count;
                }
            }
        }

        private void CopyStream(Stream input, Stream output)
        {
            if (transferType == "I")
                CopyStreamImage(input, output, 4096);
            else
                CopyStramAscii(input, output, 4096);
        }

        private string PrintWorkingDirectory()
        {
            string current = currentDirectory.Replace(root, string.Empty);

            if (current.Length == 0)
            {
                current = "/";
            }

            return $"257 \"{current}\" is current working directory.";
        }

        private string FileSize(string path)
        {
            path = NormalizeFileName(path);

            if (path != null)
            {
                if (File.Exists(path))
                {
                    long length = 0;

                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        length = fs.Length;
                    }

                    return $"213 {length.ToString()}";
                }
            }

            return "550 File not found.";
        }

        private string Delete(string path)
        {
            path = NormalizeFileName(path);

            if (path != null && File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    return "450 File action not taken (check your rights).";
                }
                return "250 File deleted successfully";
            }
            return "550 File not found.";
        }

        private string RemoveDir(string path)
        {
            if (path == null)
                path = string.Empty;
            else
            {
                if (path == "/")
                {
                    path = root;
                }
                else
                {
                    if (path.StartsWith("/"))
                    {
                        path = Path.Combine(root, path.Substring(1));
                    }
                    else
                    {
                        path = Path.Combine(currentDirectory, path);
                    }
                }
            }

            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch
                {
                    return "450 File action not taken (check your rights).";
                }
                return "250 Directory removed successfully";
            }
            return "550 Directory not found";
        }

        private string MakeDir(string path)
        {
            if (path == null)
                return "550 Directory not found";
            else
            {
                if (path == "/")
                {
                    path = root;
                }
                else
                {
                    if (path.StartsWith("/"))
                    {
                        path = Path.Combine(root, path.Substring(1));
                    }
                    else
                    {
                        path = Path.Combine(currentDirectory, path);
                    }
                }

                if (Directory.Exists(path))
                    return "521 Directory exists";
                else
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        return "450 File action not taken (check your rights).";
                    }
                    path = path.Replace(root, string.Empty);
                    return $"257 <{path}> Directory created.";
                }
            }
        }

        private string Store(string path)
        {
            path = NormalizeFileName(path);

            if (IsPathValid(path))
            {
                if(dataConnectionType == DataConnectionType.Active)
                {
                    dataClient = new TcpClient();
                    dataClient.BeginConnect(dataEndpoint.Address, dataEndpoint.Port, DoStore, path);
                }
                else
                {
                    passiveListener.BeginAcceptTcpClient(DoStore, path);
                }

                return string.Format("150 Opening {0} mode data transfer.", dataConnectionType);
            }
            else
                return "450 File action not taken (invalid path).";
        }

        private void DoStore(IAsyncResult result)
        {
            if (dataConnectionType == DataConnectionType.Active)
            {
                dataClient.EndConnect(result);
            }
            else
            {
                dataClient = passiveListener.EndAcceptTcpClient(result);
            }

            string path = (string)result.AsyncState;

            try
            {
                using (NetworkStream dataStream = dataClient.GetStream())
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    CopyStream(dataStream, fs);
                }

            }
            catch
            {
                dataClient.Close();
                dataClient = null;

                controlWriter.WriteLine("426 Closing data connection, file transfer aborted.");
                controlWriter.Flush();
                return;
            }

            dataClient.Close();
            dataClient = null;

            controlWriter.WriteLine("226 Closing data connection, file tranfer succesful");
            controlWriter.Flush();
        }

        private string Rename(string renameFrom, string renameTo)
        {
            renameTo = NormalizeFileName(renameTo);
            renameFrom = NormalizeFileName(renameFrom);

            if ( renameTo != null && renameFrom != null )
            {
                if (File.Exists(renameFrom))
                    File.Move(renameFrom, renameTo);
                else if (Directory.Exists(renameFrom))
                    Directory.Move(renameFrom, renameTo);
                else
                    return "450 File action not taken (invalid path).";

                return "250 File renamed successfully.";
            }
            else
                return "450 File action not taken (invalid path).";
        }
        #endregion
    }
}
