using SRO_Clientless;
using SilkroadSecurityAPI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SRO_Clientless
{
    public class PacketHandler
    {
        private static Func<Client, Packet, bool>[] _handler = new Func<Client, Packet, bool>[65536];

        public static Func<Client, Packet, bool>[] Handler => _handler;

        public static void InitializeHandler()
        {
            _handler[0] = Unhandled;
            _handler[8193] = Process0x2001;
            _handler[8994] = Process0x2322;
            _handler[41763] = Process0xa323;
            _handler[41218] = Process0xa102;
            _handler[12320] = Process0x3020;
            _handler[45057] = Process0xb001;
            _handler[24581] = Process0x6005;
            _handler[41219] = Process0xa103;
            _handler[0xB007] = Process0xb007;
            _handler[13493] = Process0x34B5;
            _handler[0x7001] = Process0x7001;
            _handler[0xB06B] = Process0xB06B;
            _handler[0x34A6] = Process0x34A6;

        }

        private static bool Process0x34A6(Client c, Packet p)
        {
            Console.WriteLine($"[{c._index}] has spawned successfully with character [{c.charname}]");
            Packet createparty = new Packet(0x7069);
            createparty.WriteUInt32(0); // static
            createparty.WriteUInt32(0); // static
            createparty.WriteUInt8(7); // party property flag (0 = exp free/item free/master only can invite | 1 = exp share/item free/master only can invite | 2 = exp free/item share/master only can invite | 3 = exp share/item share/master only can invite | 4 = exp free/item free/any member can invite | 5 = exp share/item free/any member can invite | 6 = exp free/item share/any member can invite | 7 = exp share/item share/any member can invite)
            createparty.WriteUInt8(0); // party purpose flag (0 = Hunting | 1 = Quest | 2 = Trade | 3 = Thief Union)
            createparty.WriteUInt8(1); // party minimum level
            createparty.WriteUInt8(100); // party maximum level
            createparty.WriteAscii("MY PARTY IS ACTIVE " + c.charname); // party title
            c.SendPacket(createparty);
            return false;
        }

        private static bool Process0xB06B(Client c, Packet p)
        {
            if (p.ReadUInt8() == 1)
            {
                Packet createparty = new Packet(0x7069);
                createparty.WriteUInt32(0); // static
                createparty.WriteUInt32(0); // static
                createparty.WriteUInt8(7); // party property flag (0 = exp free/item free/master only can invite | 1 = exp share/item free/master only can invite | 2 = exp free/item share/master only can invite | 3 = exp share/item share/master only can invite | 4 = exp free/item free/any member can invite | 5 = exp share/item free/any member can invite | 6 = exp free/item share/any member can invite | 7 = exp share/item share/any member can invite)
                createparty.WriteUInt8(0); // party purpose flag (0 = Hunting | 1 = Quest | 2 = Trade | 3 = Thief Union)
                createparty.WriteUInt8(1); // party minimum level
                createparty.WriteUInt8(100); // party maximum level
                createparty.WriteAscii("MY PARTY IS ACTIVE " + c.charname); // party title
                c.SendPacket(createparty);

                Console.WriteLine($"[{c._index}] Party matching has been created!");
            }
            return false;
        }

        private static bool Process0x7001(Client c, Packet p)
        {
            c.charname = p.ReadAscii();
            Console.WriteLine($"[{c._index}] ({c.charname}) has been selected.");
            return false;
        }

        private static bool Unhandled(Client c, Packet p)
        {
            return false;
        }

        private static bool Process0x2001(Client c, Packet p)
        {
            if (c._clientType == ClientType.GatewayServer)
            {
                Packet packet = new Packet(24834, true);
                packet.WriteUInt8(22);
                packet.WriteAscii(c._username);
                packet.WriteAscii(c._password);
                packet.WriteUInt16(64);
                c.SendPacket(packet);
            }
            return false;
        }

        private static bool Process0x2322(Client c, Packet p)
        {
            Packet packet = new Packet(25379);
            packet.WriteAscii("1");
            c.SendPacket(packet);
            return false;
        }

        private static bool Process0xa323(Client c, Packet p)
        {
            if (p.ReadUInt8() != 1)
            {
                Console.WriteLine("wrong captcha");
            }
            return false;
        }

        private static bool Process0xa102(Client c, Packet p)
        {
            switch (p.ReadUInt8())
            {
                case 1:
                    {
                        c._sessionId = p.ReadUInt32();
                        string serverIP = p.ReadAscii();
                        ushort serverPort = p.ReadUInt16();

                        //Console.WriteLine($"Attemping login, index {c._index}, struserid {c._username}, ip {serverIP}, port {serverPort}");
                        new Client(c._index, ClientType.AgentServer, c._username, c._password, c._sessionId, c._serverIP, serverPort).Connect();
                        //new Client(c._index, ClientType.AgentServer, c._username, c._password, c._sessionId, c._serverIP, serverPort).Connect();
                        break;
                    }
                case 2:
                    switch (p.ReadUInt8())
                    {
                        case 1:
                            p.ReadUInt8();
                            p.ReadUInt8();
                            p.ReadUInt8();
                            p.ReadUInt8();
                            p.ReadUInt8();
                            Console.WriteLine("The password is not correct, please make sure that you have entered the right pass.");
                            break;
                        case 2:
                            if (p.ReadUInt8() == 1)
                            {
                                Console.WriteLine("The account is blocked reason: " + p.ReadAscii());
                            }
                            break;
                        case 3:
                            Console.WriteLine("I'm already logged in.");
                            break;
                    }
                    break;
            }
            return false;
        }

        private static bool Process0x3020(Client c, Packet p)
        {
            c.SendPacket(new Packet(12306));
            c.SendPacket(new Packet(29966));

            Console.WriteLine($"[{c._index}] ({c.charname}) has been spawned.");

            return false;
        }

        private static bool Process0xb001(Client c, Packet p)
        {
            if (p.ReadUInt8() != 1)
            {
                throw new Exception("char failed to start");
            }
            return false;
        }

        private static bool Process0x6005(Client c, Packet p)
        {
            if (c._clientType == ClientType.AgentServer)
            {
                Packet packet = new Packet(24835, true);
                packet.WriteUInt32(c._sessionId);
                packet.WriteAscii(c._username);
                packet.WriteAscii(c._password);
                packet.WriteUInt8(22);
                packet.WriteUInt8Array(new byte[6]);
                c.SendPacket(packet);
            }
            return false;
        }

        private static bool Process0xa103(Client c, Packet p)
        {
            if (c._clientType == ClientType.AgentServer && p.ReadUInt8() == 1)
            {
                Packet packet = new Packet(28679);
                packet.WriteUInt8(2);
                c.SendPacket(packet);
            }
            return false;
        }

        private static bool Process0xb007(Client c, Packet p)
        {
            byte action = p.ReadUInt8();
            byte resultflag = p.ReadUInt8();
            if (action == 2)
            {
                //    case 2:
                //        if (p.ReadUInt8() == 1 && p.ReadUInt8() > 0)
                //        {
                //            p.ReadUInt32();
                //            string value = p.ReadAscii();
                //            Packet packet = new Packet(28673);
                //            p.WriteAscii(value);
                //            c.SendPacket(packet);
                //        }
                //        break;
                if (resultflag == 1)
                {
                    List<string> CharactersFound = new List<string>();
                    byte num2 = p.ReadUInt8();
                    for (int i = 0; i < num2; i++)
                    {
                        uint num8;
                        byte num9;
                        uint num4 = p.ReadUInt32();
                        string item = p.ReadAscii();
                        p.ReadUInt8();
                        p.ReadUInt8();
                        p.ReadUInt64();
                        p.ReadUInt16();
                        p.ReadUInt16();
                        p.ReadUInt16();
                        p.ReadUInt32();
                        p.ReadUInt32();
                        if (p.ReadUInt8() == 1)
                        {
                            p.ReadUInt32();
                        }
                        p.ReadUInt16();
                        p.ReadUInt8();
                        byte num6 = p.ReadUInt8();
                        int num7 = 0;
                        while (num7 < num6)
                        {
                            num8 = p.ReadUInt32();
                            num9 = p.ReadUInt8();
                            num7++;
                        }
                        byte num10 = p.ReadUInt8();
                        for (num7 = 0; num7 < num10; num7++)
                        {
                            num8 = p.ReadUInt32();
                            num9 = p.ReadUInt8();
                        }
                        CharactersFound.Add(item);
                    }
                    string plannedcharacter = c._username;
                    if (!CharactersFound.Contains(plannedcharacter))
                    {
                        //if (Globals.MainWindow.AutoCreate)
                        //{
                        Console.WriteLine($"[{c._index}] Character not found, creating a new character...");

                        Packet checkname = new Packet(0x7007);
                        checkname.WriteUInt8(0x04); // flag
                        checkname.WriteAscii(plannedcharacter);
                        c.SendPacket(checkname);
                        //}
                    }
                    else
                    {
                        Packet packet = new Packet(28673);
                        packet.WriteAscii(plannedcharacter);
                        c.SendPacket(packet);

                        c.charname = plannedcharacter;
                        Console.WriteLine($"[{c._index}] ({plannedcharacter}) has been selected.");
                    }
                }
            }
            else if (action == 1)
            {
                if (resultflag == 1)
                {
                    Console.WriteLine($"[{c._index}] Character [{c._username}] has been selected successfully.");
                    //Globals.MainWindow.char_list.Items.Clear();
                    Packet packet2 = new Packet(0x7007);
                    packet2.WriteUInt8((byte)2);
                    c.SendPacket(packet2);
                }
                else if (resultflag == 2)
                {
                    Console.WriteLine($"[{c._index}] Character creation failed.");
                }
            }
            else if (action == 4)
            {
                if (resultflag == 2)
                {
                    Console.WriteLine($"[{c._index}] Character name is invalid.");
                }
                else if (resultflag == 1)
                {
                    string plannedcharacter = c._username;
                    //if (Globals.MainWindow.AutoCreate)
                    //{
                    Packet createcharacter = new Packet(0x7007);
                    createcharacter.WriteUInt8(0x01); // flag
                    createcharacter.WriteAscii(plannedcharacter); // character name
                    createcharacter.WriteInt32(0x77F); // character ObjID (0x77F = CHAR_CH_MAN_WARRIOR)
                    createcharacter.WriteUInt8(0x22); // maybe related to height and weight (body)
                    createcharacter.WriteInt32(0xE3B); // Suit/Chest ObjID (0xE3B = ITEM_CH_M_CLOTHES_01_BA_A_DEF)
                    createcharacter.WriteInt32(0xE3C); // Trousers/Legs ObjID (0xE3B = ITEM_CH_M_CLOTHES_01_LA_A_DEF)
                    createcharacter.WriteInt32(0xE3D); // Shoes/Boots ObjID (0xE3B = ITEM_CH_M_CLOTHES_01_FA_A_DEF)
                    createcharacter.WriteInt32(0xE33); // Weapon ObjID (0xE3B = ITEM_CH_TBLADE_01_A_DEF)
                    c.SendPacket(createcharacter);
                    //}
                }
            }
            //switch (p.ReadUInt8())
            //{
            //    case 1:
            //        {
            //            if (p.ReadUInt8() != 1)
            //            {
            //                throw new Exception("FAILD TO CREATE CHARACTER");
            //            }
            //            Packet packet2 = new Packet(28679);
            //            packet2.WriteUInt8(2);
            //            c.SendPacket(packet2);
            //            break;
            //        }
            //    case 2:
            //        if (p.ReadUInt8() == 1 && p.ReadUInt8() > 0)
            //        {
            //            p.ReadUInt32();
            //            string value = p.ReadAscii();
            //            Packet packet = new Packet(28673);
            //            p.WriteAscii(value);
            //            c.SendPacket(packet);
            //        }
            //        break;
            //}
            return false;
        }

        private static bool Process0x34B5(Client c, Packet packet)
        {
            c.SendPacket(new Packet(13494));
            return false;
        }
    }
}
