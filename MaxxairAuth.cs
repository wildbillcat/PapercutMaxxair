/*
 * Patrick McMorran
 * 
 * C# Authentication Plugin to be used with non-strandard Machines to be
 * tracked as though they were printers. This particular plugin is inspired
 * to be used for use with Make the Cut!(tm) software, but could easily be 
 * used to track other systems as well.
 * 
 ************************************************************************* 
 *
 * This is free and unencumbered software released into the public domain.
 *
 * Anyone is free to copy, modify, publish, use, compile, sell, or
 * distribute this software, either in source code form or as a compiled
 * binary, for any purpose, commercial or non-commercial, and by any
 * means.
 * 
 * In jurisdictions that recognize copyright laws, the author or authors
 * of this software dedicate any and all copyright interest in the
 * software to the public domain. We make this dedication for the benefit
 * of the public at large and to the detriment of our heirs and
 * successors. We intend this dedication to be an overt act of
 * relinquishment in perpetuity of all present and future rights to this
 * software under copyright law.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 * 
 * For more information, please refer to <http://unlicense.org/>
 */
using System;
using CookComputing.XmlRpc;

public class MaxxairAuth {
    public static int Main() {

        /*
         * User Configuration Area
         */
        string authToken = "Password for Builtin User or API Key"; //Password of the internal Administrative user for papercut or the XML API Key if set
        string server = "localhost"; //URL of the papercut server ie. papercut.localDomain.edu or 172.2.21.5
        int port = 9191; //Standard application server port is 9191
        string printerName = "Virtual-Queue-on-Printserver"; //This should be the name of the Queue in papercut that controls the cost and permissions.
        string printerServer = "Printserver"; //This is the the name of the print server the fake print queue is sitting on to configure costs.


        /*
         * Non-User Area Below
         */

        // Creates a Server Command Proxy, provided by the Papercut.
        string user = Environment.UserName; //Windows Username of the currently logged in user.
        string computerName = Environment.MachineName;
        ServerCommandProxy serverProxy = new ServerCommandProxy(server, port, authToken);

       
        try {
            //Test to see if User Exists
            if (!serverProxy.UserExists(user))
            {
                // User does not exist! Explain and exit returning 1
                Console.WriteLine("I'm sorry but this user does not exist in the chargeback system. Please ensure you are registered for printing.");
                return 1;
            }
            //Test to see if user printing is disabled
            if (Boolean.Parse(serverProxy.GetUserProperty(user, "disabled-print")))
            {
                 Console.WriteLine("I'm sorry but your print account is disabled. Please ensure you are registered for printing.");
                 return 2;
            }                                  
            //Test to see if user can afford the print
            if(Boolean.Parse(serverProxy.GetUserProperty(user, "restricted"))){
                    // Account is restricted, make sure that it has money for the print                 
                    double printerCost = serverProxy.GetPrinterCostSimple(printerServer, printerName); // Get the user's account balance to see if they are able to afford the print
                        if (serverProxy.AdjustUserAccountBalanceIfAvailable(user, -1 * printerCost, "Test Charge for " + printerServer + "\\" + printerName, ""))
                        {
                            serverProxy.AdjustUserAccountBalance(user, printerCost, "Test Charge for " + printerServer + "\\" + printerName, "");
                        }
                        else
                        {
                            Console.WriteLine("I'm sorry but this user does not have enough money for this print on their account.");
                            return 3;
                        }
                    }
              //Charge User for the Print Job
              string printjob = "user=" + user + ",server=" + server + ",printer=" + printerName + ",client-machine=" + computerName + ",server=" + printerServer; // This assembles a string of information to submit the printjob 
              serverProxy.ProcessJob(printjob); // Sends the formatted print job
              return 0; // job was charged. Success!
                
        } catch (XmlRpcFaultException fex) {
            Console.WriteLine("Fault: {0}, {1}", fex.FaultCode, fex.FaultString);
            return -1;
        }        
    }
}
