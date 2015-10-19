using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Net;
using System.Collections.ObjectModel;

using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace HearthstoneBot
{
	public class ServerSocket
    {
        // Server setup information
        private readonly IPAddress IP = null;
        private const Int32 SERVER_PORT = 8112;

        // Work variables
        private readonly Mutex mutex;
		private readonly Thread thread;
        private volatile List<string> events;

        // TODO: Make these local variables and fix the stop function
        //          This is indeed a hack, but it works!
        private volatile TcpListener socket = null;
        private volatile TcpClient client = null;
        private volatile NetworkStream network_stream = null;
        private volatile bool shell_mode = true;
        private APICmd api_cmd = null;

        public ServerSocket()
        {
            IP = Dns.GetHostEntry("localhost").AddressList[0];

            events = new List<string>();
            thread = new Thread(new ThreadStart(run));

            mutex = new Mutex();
            api_cmd = new APICmd();
            Log.log("ServerSocket loaded!");
        }

        public void stop()
        {
            // TODO: See the todo at the variables
            if(thread != null)
                thread.Abort();
            if(network_stream != null)
                network_stream.Close();
            if(client != null)
                client.Close();
            if(socket != null)
                socket.Stop();
        }

        public void start()
        {
            thread.Start();
        }

		private void run()
		{
            Log.log("ServerSocket running!");
            // Open a server socket
            try
            {
                socket = new TcpListener(IP, SERVER_PORT);    
            }   
            catch(Exception e)
            {
                Log.error("Unable to open server socket");
                Log.error(e.ToString());
                return;
            }
            // Start listening
			socket.Start();
            Log.log("ServerSocket started!");

			try
			{
                // Message loop
				while (true)
				{
                    // Wait for a client to connect
		            client = socket.AcceptTcpClient();
                    Log.say("External connection");

                    // Open a stream to the client
                    network_stream = client.GetStream();
                    // Assert that we can read the stream
                    if(network_stream.CanRead == false)
                    {
                        // Report this incident
                        Log.error("Unreadable network stream");
                        // Cleanup
                        network_stream.Close();
                        client.Close();
                        // Wait for another connection
                        continue;
                    }
                    // At this point, the network stream is readable
                    // Read the entire client string
                    StreamReader sr = new StreamReader(network_stream);
                    while (true)
                    {
                        string command = sr.ReadLine();
                        // Report that we've got a command
                        Log.log("Got network command: " + command);
                        // Wait until it is safe to enter.
                        mutex.WaitOne();
                        // Append command to lazy list
                        events.Add(command);
                        // Release the Mutex.
                        mutex.ReleaseMutex();

                        if (!shell_mode)
                        {
                            Log.log("we will close!");
                            // At this point, we're done;
                            // Close the network stream
                            network_stream.Close();
                            // Close connection to client
                            client.Close();
                            break;
                        }
                    }
                    
				}
			}
			catch(Exception e)
			{
                Log.error("Error in ServerSocket message loop");
				Log.error(e.StackTrace);
			}

            Log.log("ServerSocket stopped!");
            // Stop listening
            socket.Stop();
		}

        public void handle_events()
        {
            // Wait until it is safe to enter.
            mutex.WaitOne();
            // Handle all events passed, since last visit
            foreach(string evnt in events)
            {
                handle_event(evnt);
            }
            // Remove all events for list
            events.Clear();
            // Release the Mutex.
            mutex.ReleaseMutex();
        }

        private void handle_event(string data)
        {
            // Convert entire string to ascii
            string ascii_data = Utils.EncodeNonAsciiCharacters(data);
            handle_event_internal(ascii_data);
        }

		private void handle_event_internal(string data)
		{
            StreamWriter sw = new StreamWriter(network_stream);
            // Stop the bot
			if (data.Contains("stop_bot"))
			{
                Plugin.setRunning(false);
			}
            // Start the bot
            else if (data.Contains("start_bot"))
			{
                Plugin.setRunning(true);
			}
            // Set bot path
            else if (data.Contains("path="))
			{
                // Find the index of the '='
                int index_of_equals = data.IndexOf('=');
                // Find the path substring
                string path = data.Substring(index_of_equals+1);

                // Remove newline character and trim '\0' character.
                string no_endline = Regex.Replace(path, @"\t|\n|\r|\0", "");
                string unix_slashes = no_endline.Replace('/', '\\');

                // Set this within the plugin, also update the log path
                Plugin.setBotPath(unix_slashes);
                Log.new_log_directory(Plugin.getBothPath() + "logs/");
                
                // Reload the bot, with the new path
                Plugin.loadAIBot();
			}
            // Reload AI Scripts
            else if (data.Contains("reload_scripts"))
			{
                Plugin.ReloadScripts();
			}
            else if (data.Contains("mode="))
            {
                // Find the index of the '='
                int index_of_equals = data.IndexOf('=');
                // Find the path substring
                string actual_mode = data.Substring(index_of_equals+1);
                // Find the corresponding mode
                AIBot.Mode mode = (AIBot.Mode) Enum.Parse(typeof(AIBot.Mode), actual_mode);        
                if (Enum.IsDefined(typeof(AIBot.Mode), mode))
                {
                    Plugin.setMode(mode);
                }
                else
                {
                    Log.error("While trying to set mode:");
                    Log.error("Unknown mode = " + actual_mode);
                }
            }
            else if (data.Contains("get_cry"))
            {
                sw.WriteLine("ememy: " + api_cmd.getCrystals("ENEMY_HERO") + " $ " + "me: " + api_cmd.getCrystals("OUR_HERO"));
                sw.Flush();
            }
            else if (data.Contains("shell"))
            {
                shell_mode = true;
                sw.WriteLine("shell succc");
                sw.Flush();
            }
            else
            {
                Log.error("Unknown network command : " + data);
            }
		}
    }
}
