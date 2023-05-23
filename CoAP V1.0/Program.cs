using System;
using System.Threading;
using System.Collections.Generic;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Gre;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.Igmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Core.Extensions;
using System.Net;
using System.Net.NetworkInformation;
using System.Timers;
using System.Text;

namespace CoAP_V1._0
{
    class Program
    {
        static Dictionary</*ushort*/ int, Packet> Cevaplar = new Dictionary<int/*ushort*/, Packet>();
        static MacAddress sourceMAC;
        static MacAddress destinationMAC;
        static string sourceIP_str;
        static IpV4Address sourceIP;
        static IpV4Address destinationIP;
        static LivePacketDevice selectedDevice;
        static Dictionary<ushort, DateTime> pingID = new Dictionary<ushort, DateTime>();
        static string message = "null";
        static int ms = -1;
        static byte paketDizisi;


        static void Main(string[] args) /*main*/
        {

            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;


            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Print the list
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.Write((i + 1) + ". " + device.Name);
                if (device.Description != null)
                    Console.WriteLine(" (" + device.Description + ")");
                else
                    Console.WriteLine(" (No description available)");
            }

            int deviceIndex = 0;
            do
            {
                Console.WriteLine("Enter the interface number (1-" + allDevices.Count + "):");
                string deviceIndexString = Console.ReadLine();
                if (!int.TryParse(deviceIndexString, out deviceIndex) ||
                    deviceIndex < 1 || deviceIndex > allDevices.Count)
                {
                    deviceIndex = 0;
                }
            } while (deviceIndex == 0);
            // Take the selected adapter
            selectedDevice = allDevices[deviceIndex - 1];

            sourceMAC = selectedDevice.GetMacAddress(); //secilen cihazin MAC adresini otomatik olarak alir
            destinationMAC = new MacAddress("[address here]"); //pyshical address of default gateway

            sourceIP_str = null;//boş string

            foreach (DeviceAddress address in selectedDevice.Addresses)
            {
                if (address.Address.Family == SocketAddressFamily.Internet) //internet = ipv4 idi
                    sourceIP_str = address.Address.ToString().Substring(9, address.Address.ToString().Length - 9);
                //Console.WriteLine(address.Address.ToString());
            }


            sourceIP = new IpV4Address(sourceIP_str); //bu da hazır vardır
            destinationIP = new IpV4Address(sourceIP_str);

            Thread thread2 = new Thread(Listener);
            Thread thread1 = new Thread(MsgSender);

            thread1.Start();
            thread2.Start();
        }






        static void Listener()
        {
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {

                // Compile the filter
                using (BerkeleyPacketFilter filter = communicator.CreateFilter("ip and src " + sourceIP.ToString())) //sourceIP.ToString()
                {
                    communicator.SetFilter(filter);
                }
                Console.WriteLine("System ready for I/O...");

                // Retrieve the packets
                Packet p;
                int id = 0;
                try
                {
                    do
                    {
                        PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out p);
                        switch (result)
                        {
                            case PacketCommunicatorReceiveResult.Timeout:
                                continue;
                            case PacketCommunicatorReceiveResult.Ok:
                                IpV4Datagram ip = p.Ethernet.IpV4;
                                UdpDatagram udp = ip.Udp;
                                //DnsDatagram dns = udp.Dns;
                                id++;


                                lock (Cevaplar) //locked the "Cevaplar" aka recieved because its a critical section
                                {
                                    Cevaplar.Add(id, p); //An item with the same key has already been added. Key:0
                                }
                                break;
                            default:
                                throw new InvalidOperationException("The result " + result + " should never be reached here");
                        }
                    } while (true);
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }





        private static void MsgSender() /*Sends a CoAP protocol inside a UDP packet*/
        {

            Console.WriteLine("0-Cikis yap");
            Console.WriteLine("1-Sicaklik okuma");
            Console.WriteLine("2-Nem okuma");
            Console.WriteLine("3-Isik durumunu gönder");
            Console.WriteLine("4-Ses olc");
            Console.WriteLine("5-Sensorleri ac");
            Console.WriteLine("6-Sensorleri kapa");

            //message = Console.ReadLine();
            /*case yazan yerlerin  8 bitlik (1 baytlık) alanları 32 bit integar a çevir (anlamayacak kadar beyin yoksunuysan 2 haneli sayı demek).*/

            do
            {
                ms = int.Parse(Console.ReadLine());

                switch (ms) //Makes the second 4 bytes of the 8 byte packet depending on the choice.
                {
                    case 0: //a custom escape system with a fake loading screen
                        Console.Write("Closing Program. ");
                        Thread.Sleep(500);
                        Console.Write(". ");
                        Thread.Sleep(500);
                        Console.Write(".");
                        Thread.Sleep(500);
                        Environment.Exit(0);
                        break;
                    case 1:
                        paketDizisi = 0; //00000000
                        break;
                    case 2:
                        paketDizisi = 16; //00010000
                        break;
                    case 3:
                        paketDizisi = 32; //00100000
                        break;
                    case 4:
                        paketDizisi = 48; //00110000
                        break;
                    case 5:
                        paketDizisi = 64; //01000000
                        break;
                    case 6:
                        paketDizisi = 80; //01010000
                        break;
                    default:
                        Console.WriteLine("The command you're trying to reach does not exist.");
                        Console.Write("Please try again: ");
                        break;
                }

            }
            while (ms <= 0 || ms > 6);




                    using (PacketCommunicator communicator = selectedDevice.Open(200,
                                                                         PacketDeviceOpenAttributes.Promiscuous,
                                                                         1000))
            {
                for (ushort i = 0; i < 1; i++) //"ping" command pings 4 times.
                {
                    var paketveID = BuildUdpPacket(i, i);
                    //Console.WriteLine(paketveID.Item2.ToString());
                    pingID.Add(i, DateTime.Now);
                    communicator.SendPacket(paketveID.Item1);

                    var t = new Thread(() => Yorumla(i));
                    i++;
                    t.Start();
                    Thread.Sleep(1000);
                }
            }


        }
        private static Tuple<Packet, ushort> BuildUdpPacket(ushort ID = 0, ushort Identifier = 0) //UDP packet building
        {
            byte a = 0;
            byte b = 80;
            byte c = 255;
            byte[] array = { b , paketDizisi , a, a};
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = sourceMAC,
                    Destination = destinationMAC,
                    EtherType = EthernetType.None, // Will be filled automatically.
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = sourceIP,
                    CurrentDestination = destinationIP,
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null, // Will be filled automatically.
                    Identification = Identifier,
                    Options = IpV4Options.None,
                    Protocol = null, // Will be filled automatically.
                    Ttl = 100,
                    TypeOfService = 0,
                };

            UdpLayer udpLayer =
                new UdpLayer
                {
                    SourcePort = 4050,
                    DestinationPort = 5683,
                    Checksum = null, // Will be filled automatically.
                    CalculateChecksumValue = true,
                };

            PayloadLayer payloadLayer =
               new PayloadLayer
               {
                   Data = new Datagram(array),//Our packetArray data was originally a byte containing int32, so we converted it into a bit array. 80
                   //Encoding.Default.GetBytes(),
               };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, udpLayer, payloadLayer);

            return Tuple.Create(builder.Build(DateTime.Now), ID = Identifier);
        }








        static void Yorumla(ushort ID)
        {
            Thread.Sleep(2500);
            Packet p;

            try
            {
                lock (Cevaplar)
                {
                    p = Cevaplar[ID];
                }
            }
            catch
            {
                Console.WriteLine("ZAMAN ASIMI!");
                return;
            }

            IpV4Datagram ip = p.Ethernet.IpV4;
            UdpDatagram udp = ip.Udp;
            string udpDatagram = udp.Payload.ToHexadecimalString();
            string codeHex = udpDatagram.Substring(2, 2);
            string codeDec = ParseToDecimalIPformat(codeHex);


            //Console.WriteLine(udpDatagram);


            Console.WriteLine("mesaj " + codeDec.ToString() + " mesajı alındı.");

        }
        private static byte ConvertBoolArrayToByte(bool[] source)
        {

            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - source.Length;

            // Loop through the array
            foreach (bool b in source)
            {
                // if the element is 'true' set the bit at that position
                if (b)
                    result |= (byte)(1 << (7 - index));

                index++;
            }


            return result;
        }

        static string ParseToDecimalIPformat(string hexDataString)
        {
            string toplam = "";

            for (int i = 0; i < hexDataString.Length; i = i + 2)
            {
                int deger = int.Parse(hexDataString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);

                toplam += deger.ToString();
                if (i == hexDataString.Length - 2)
                {
                    continue;
                }
                toplam += ".";
            }

            return toplam;

        }


    }

}



