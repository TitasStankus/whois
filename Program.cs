// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using whois;

Boolean debug = false;

Dictionary<String, User> DataBase = new Dictionary<String, User>
{
    {"cssbct",
      new User {UserID="cssbct",Surname="Tompsett",Fornames="Brian C",Title="Eur Ing",
        Position="Lecturer of Computer Science",
        Phone="+44 1482 46 5222",Email="B.C.Tompsett@hull.ac.uk",Location="in RB-336" }
   }
};

if (args.Length == 0)
{
    Console.WriteLine("Starting Server");
    RunServer();
}
else
{
    for (int i = 0; i < args.Length; i++)
    {
        ProcessCommand(args[i]);
    }
}

/// Initiate the Web Server task
void RunServer()
{
    TcpListener listener;
    Socket connection;
    NetworkStream socketStream;
    try
    {
        listener = new TcpListener(443);
        listener.Start();
        while (true)
        {
            if (debug) Console.WriteLine("Server Waiting connection...");
            connection = listener.AcceptSocket();
            socketStream = new NetworkStream(connection);
            doRequest(socketStream);
            socketStream.Close();
            connection.Close();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
    if (debug)
        Console.WriteLine("Terminating Server");
}
/// Handle a network request
void doRequest(NetworkStream socketStream)
{
    StreamWriter sw = new StreamWriter(socketStream);
    StreamReader sr = new StreamReader(socketStream);

    if (debug) Console.WriteLine("Waiting for input from client...");
    try
    {
        String line = sr.ReadLine();

        if (line == null)
        {
            if (debug) Console.WriteLine("Ignoring null command");
            return;
        }

        Console.WriteLine($"Received Network Command: '{line}'");

        //sw.WriteLine(line);   // Inhibit when working
        //sw.Flush();           // Inhibit when working  

        if (line == "POST / HTTP/1.1")
        {
            // The we have an update
            if (debug) Console.WriteLine("Received an update request");

            //DataBase["cssbct"].Location = "network update test string"; // Testing

            int content_length = 0;
            while (line != "")
            {
                if (line.StartsWith("Content-Length: "))
                {
                    content_length = Int32.Parse(line.Substring(16));
                }
                line = sr.ReadLine();
                if (debug) Console.WriteLine($"Skipped Header Line: '{line}'");
            }

            while (line != "") line = sr.ReadLine(); // Skip to blank line

            line = "";
            for (int i = 0; i < content_length; i++) line += (char)sr.Read();

            String[] slices = line.Split(new char[] { '&' }, 2);
            if (slices.Length < 2 || !slices[0].StartsWith("name=") || !slices[1].StartsWith("location="))
            {
                // This is an invalid request
                sw.WriteLine("HTTP/1.1 400 Bad Request");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.Flush();
                Console.WriteLine($"Unrecognised command: '{line}'");
                return;
            }
            String ID = slices[0].Substring(5);
            String value = slices[1].Substring(9);
            if (debug) Console.WriteLine($"Received an update request for '{ID}' to '{value}'");
            if (!DataBase.ContainsKey(ID)) DataBase.TryAdd(ID, new User { });
            DataBase[ID].Location = value;

            sw.WriteLine("HTTP/1.1 200 OK");
            sw.WriteLine("Content-Type: text/plain");
            sw.WriteLine();
            sw.Flush();
            Console.WriteLine($"Performed Update on '{ID}' location to '{value}'");
            //Console.WriteLine($"New database location: {DataBase[ID].Location}"); // Testing

        }
        else if (line.StartsWith("GET /?name=") && line.EndsWith(" HTTP/1.1"))
        {
            // then we have a lookup
            if (debug) Console.WriteLine("Received a lookup request");

            String[] slices = line.Split(" ");  // Split into 3 pieces
            String ID = slices[1].Substring(7);  // start at the 7th letter of the middle slice - skip `/?name=`

            if (DataBase.ContainsKey(ID))
            {
                String result = DataBase[ID].Location;

                sw.WriteLine("HTTP/1.1 200 OK");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.WriteLine(result);
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '{result}'");
            }
            else
            {
                // Not found
                sw.WriteLine("HTTP/1.1 404 Not Found");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '404 Not Found'");
            }
        }
        else
        {
            // We have an error
            Console.WriteLine($"Unrecognised command: '{line}'");
            sw.WriteLine("HTTP/1.1 400 Bad Request");
            sw.WriteLine("Content-Type: text/plain");
            sw.WriteLine();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Fault in Network Processing: {e.ToString()}");
    }
    finally
    {
        sw.Close();
        sr.Close();
    }
}

/// Process the next database command request
void ProcessCommand(string command)
{
    if (debug) Console.WriteLine($"\nCommand: {command}");
    try
    {
        String[] slice = command.Split(new char[] { '?' }, 2);
        String ID = slice[0];
        String operation = null;
        String update = null;
        String field = null;

        if (slice.Length == 2)
        {
            operation = slice[1];
            // Could be empty if no field specified
            if (operation == "")
            {
                // Is a record delete command
                Delete(ID);
                return;
            }
            String[] pieces = operation.Split(new char[] { '=' }, 2);
            field = pieces[0];
            if (pieces.Length == 2) update = pieces[1];
        }
        if (debug) Console.Write($"Operation on ID '{ID}'");
        if (operation == null ||
            update == null &&
            (!DataBase.ContainsKey(ID)))
        {
            Console.WriteLine($"User '{ID}' not known");
            return;
        }
        if (operation == null) Dump(ID);
        else if (update == null) Lookup(ID, field);
        else Update(ID, field, update);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Fault in Command Processing: {e.ToString()}");
    }

    /// Functions to process database requests
    void Dump(String ID)
    {
        if (debug) Console.WriteLine(" output all fields");
        try
        {
        Console.WriteLine($"UserID={DataBase[ID].UserID}");
        Console.WriteLine($"Surname={DataBase[ID].Surname}");
        Console.WriteLine($"Fornames={DataBase[ID].Fornames}");
        Console.WriteLine($"Title={DataBase[ID].Title}");
        Console.WriteLine($"Position={DataBase[ID].Position}");
        Console.WriteLine($"Phone={DataBase[ID].Phone}");
        Console.WriteLine($"Email={DataBase[ID].Email}");
        Console.WriteLine($"location={DataBase[ID].Location}");
        }
        catch (Exception e){ Console.WriteLine("Got " +e +" in database Dump"); }
    }
    void Lookup(String ID, String field)
    {
        try
        {
            if (debug)
                Console.WriteLine($" lookup field '{field}'");
            String result = null;
            switch (field)
            {
                default: Console.WriteLine($"Unknown field name: '{field}'"); return;
                case "location": result = DataBase[ID].Location; break;
                case "UserID": result = DataBase[ID].UserID; break;
                case "Surname": result = DataBase[ID].Surname; break;
                case "Fornames": result = DataBase[ID].Fornames; break;
                case "Title": result = DataBase[ID].Title; break;
                case "Phone": result = DataBase[ID].Phone; break;
                case "Position": result = DataBase[ID].Position; break;
                case "Email": result = DataBase[ID].Email; break;
            }
            Console.WriteLine(result);
        } catch (Exception e){ Console.WriteLine("Got " +e +" in database Lookup"); 
        }

    }
    void Update(String ID, String field, String update)
    {
        try
        {
            if (debug)
                Console.WriteLine($" update field '{field}' to '{update}'");
            if (!DataBase.ContainsKey(ID)) {
                DataBase.Add(ID, new User { });
            }
            switch (field)
            {
                default: Console.WriteLine($"Unknown field name: '{field}'"); return;
                case "location": DataBase[ID].Location = update; break;
                case "UserID": DataBase[ID].UserID = update; break;
                case "Surname": DataBase[ID].Surname = update; break;
                case "Fornames": DataBase[ID].Fornames = update; break;
                case "Title": DataBase[ID].Title = update; break;
                case "Phone": DataBase[ID].Phone = update; break;
                case "Position": DataBase[ID].Position = update; break;
                case "Email": DataBase[ID].Email = update; break;
            }
            Console.WriteLine("OK");
        } catch (Exception e){ Console.WriteLine("Got " +e +" in database Update");
        }
    }
    void Delete(String ID)
    {
        if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");
        DataBase.Remove(ID);
    }
}
