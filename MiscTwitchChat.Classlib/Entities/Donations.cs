using System;

namespace MiscTwitchChat.Classlib.Entities
{
    public class FundraiserData
    {
        public Fundraiser fundraiser { get; set; }
    }
    public class Fundraiser
    {
        public Donations donations { get; set; }
    }

    public class Donations
    {
        public Edge[] edges { get; set; }
    }

    public class Edge
    {
        public string cursor { get; set; }
        public Node node { get; set; }
    }

    public class Node
    {
        public string id { get; set; }
        public string name { get; set; }
        public DateTime createdAt { get; set; }
        public Amount amount { get; set; }
    }

    public class Amount
    {
        public double amount { get; set; }
        public string currencyCode { get; set; }
    }

}