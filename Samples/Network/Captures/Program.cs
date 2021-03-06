﻿namespace NetworkCaptures
{
    using System;
    using System.Reflection;
    using Tx.Network;
    using System.Linq;
    using Tx.Network.Snmp;
    using System.Reactive;

    class Program
    {
        const string fileName = "snmp.pcapng"; // subset of the packets from the WireShark sample for SNMP
        static void Main()
        {

            RawBlocks();            // raw blocks, such as section header, interface description and packets
            CapturedPackets();      // just the captured packets, without interpreting the protocol data
            UdpPackets();           // interpret as UDP packets
            Asn1Encoding();         // interpret as UDP packets with Asn1 encoding (no knowledge of SNMP protocol)
            Snmp();                 // interpret as SNMP protocol data units
        }

        static void RawBlocks()
        {
            Console.WriteLine("\n======================= {0} =======================\n", MethodInfo.GetCurrentMethod().Name);

            foreach (var block in PcapNg.ReadForward(fileName).Take(5))
                Console.WriteLine("{0} {1}", block.Length, block.Type);
        }

        static void CapturedPackets()
        {
            Console.WriteLine("\n======================= {0} =======================\n", MethodInfo.GetCurrentMethod().Name);

            var packets = PcapNg.ReadForward(fileName)
                .Where(b => b.Type == BlockType.EnhancedPacketBlock)
                .Cast<EnhancedPacketBlock>()
                .Take(5);

            foreach (var packet in packets)
                Console.WriteLine("{0} {1} {2}", packet.TimestampUtc, packet.PacketLen, packet.CapturedLen);
        }

        static void UdpPackets()
        {
            Console.WriteLine("\n======================= {0} =======================\n", MethodInfo.GetCurrentMethod().Name);

            var packets = PcapNg.ReadForward(fileName)
                .Where(b => b.Type == BlockType.EnhancedPacketBlock)
                .Cast<EnhancedPacketBlock>()
                .Take(5);

            foreach (var packet in packets)
            {
                int ipLen = packet.PacketData.Length - 14; // 14 is the size of the Ethernet header

                var ipPacket = PacketParser.Parse(
                    DateTimeOffset.UtcNow,
                    false,
                    packet.PacketData,
                    14,
                    ipLen);

                Console.WriteLine(ipPacket.PacketData.Array.ToHexDump());
                Console.WriteLine();
            }
        }

        static void Asn1Encoding()
        {
            Console.WriteLine("\n======================= {0} =======================\n", MethodInfo.GetCurrentMethod().Name);

            var packets = PcapNg.ReadForward(fileName)
                .Where(b => b.Type == BlockType.EnhancedPacketBlock)
                .Cast<EnhancedPacketBlock>()
                .Take(5);
 
            foreach (var packet in packets)
            {
                int snmpLen = packet.PacketData.Length - 42; // 42 is the size of Ethernet + IP + UDP headers

                byte[] datagram = new byte[snmpLen];
                Array.Copy(packet.PacketData, 42, datagram, 0, snmpLen);

                Console.WriteLine(datagram.ToHexDump());
            }
        }

        static void Snmp()
        {
            Console.WriteLine("\n======================= {0} =======================\n", MethodInfo.GetCurrentMethod().Name);
            var snmp = PcapNg.ReadForward(fileName)
                    .ParseSnmp()
                    .Take(5);

            foreach (var pdu in snmp)
                Console.WriteLine(pdu.ToString());
        }

    }
}
