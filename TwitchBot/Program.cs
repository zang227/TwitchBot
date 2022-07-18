using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;


namespace TwitchBot
{
    class Program
    {
        static bool RaffleComplete = false;
        static bool RaffleStarted = false;
        static bool Cancel = false;
        static String winner;
        static List<string> entrants = new List<string>();
        static List<string> pickable = new List<string>();
        static Dictionary<string, Wallet> wallets = new Dictionary<string, Wallet>();
        static String[] admins = { "rare_gangster", "zang227"};
        static String[] mods = { "rare_gangster","zang227", "b__flem", "joyewastaken", "jemapellefendi", "mattmarmalade"};
        static async Task RaffleTime(int time,Bot twitchBot)
        {
            var rand = new Random();
            int winnerIndex;
            if (time == 0)
                time = 180;
            await twitchBot.SendMessage($"/me A Raffle has started. Use !join to enter! You have {time} seconds to enter!");
            for(int i =((time * 1000) - 100); i >= 0; i -= 100)
            {
                await Task.Delay(100);
                if(i%30000 == 0)
                {
                    if (i >= 30000)
                        await twitchBot.SendMessage($"/me The raffle will end in {(i / 1000)} seconds! Type !join to enter!");
                }
                if (Cancel)
                {
                    await twitchBot.SendMessage($"/me The raffle has been cancelled!");
                    RaffleComplete = false;
                    RaffleStarted = false;
                    Cancel = false;
                    entrants.Clear();
                    pickable.Clear();
                    return;
                }

            }
            RaffleComplete = true;
            if(entrants.Count > 0)
            {
                pickable = entrants.Distinct().ToList();
                winnerIndex = rand.Next(pickable.Count);
                winner = pickable[winnerIndex];
                pickable.RemoveAt(winnerIndex);
                await twitchBot.SendMessage($"/announce @rare_gangster The raffle has ended and the winner is @{winner}");
                RaffleComplete = true;
            }
            else
            {
                await twitchBot.SendMessage($"/me The raffle has ended and no one entered.");
            }
            
            RaffleStarted = false;
            return;
        }
        static void AddEntrant(string s)
        {
            entrants.Add(s);
        }
        public static bool CheckMod(String sender)
        {
            for(int i = 0; i < mods.Length; i++)
            {
                if (String.Equals(sender.ToLower(), mods[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckAdmin(String sender)
        {
            for (int i = 0; i < admins.Length; i++)
            {
                if (String.Equals(sender.ToLower(), admins[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool walletCheck(string name)
        {
            if (!wallets.ContainsKey(name))
            {
                Wallet wallet = new Wallet(name);
                wallets.Add(name, wallet);
                return false;
            }
            else
                return true;
        }

        static void loadWallets()
        {
            string[] lines = System.IO.File.ReadAllLines("Wallets.txt");
            String[] split;
            Wallet wallet;
            int value;
            if(lines.Length > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    split = lines[i].Split(" ");
                    walletCheck(split[0]);
                    wallet = wallets[split[0]];
                    for (int j = 1; j < split.Length - 1; j += 2)
                    {
                        Int32.TryParse(split[j + 1], out value);
                        wallet.addItem(split[j], value);
                    }
                }
                Console.WriteLine("Wallets Loaded");
            }
            
        }

        static String itemConvert(string name)
        {
            switch (name.ToLower())
            {
                case "deathcard":
                    return "DeathCard";
                case "pighat":
                    return "PigHat";
                case "soda":
                    return "Soda";
                case "timeout":
                    return "TimeOut";
                case "sos":
                    return "SOS";
                case "kittytreat":
                    return "KittyTreat";
                case "hoofhands":
                    return "HoofHands";
                case "shot":
                    return "Shot";
                case "cc":
                    return "CC";
                case "onesie":
                    return "Onesie";
                case "fendiban":
                    return "FendiBan";
                case "luigi":
                    return "Luigi";
                case "other":
                    return "Other";
                default:
                    return "";
            }
        }

        static async Task saveWallets()
        {
            StreamWriter sw;
            while (true)
            {
                await Task.Delay(300000);
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("");
                Console.WriteLine("Saving wallets!");
                Console.WriteLine("");
                Console.WriteLine("---------------------------------------");
                sw = new StreamWriter("Wallets.txt");
                foreach (var item in wallets)
                {
                    sw.Write(item.Key);
                    sw.Write(" ");
                    Dictionary<string, int> wallet = wallets[item.Key].getDict();
                    foreach(var item2 in wallet)
                    {
                        sw.Write(item2.Key);
                        sw.Write(" ");
                        sw.Write(item2.Value);
                        sw.Write(" ");

                    }
                    sw.WriteLine();
                }
                sw.Close();
            }
        }


        static async Task saveWalletsManual(Bot twitchBot)
        {
            StreamWriter sw;
            Console.WriteLine("Saving wallets!");
            sw = new StreamWriter("Wallets.txt");
            foreach (var item in wallets)
            {
                sw.Write(item.Key);
                sw.Write(" ");
                Dictionary<string, int> wallet = wallets[item.Key].getDict();
                foreach (var item2 in wallet)
                {
                     sw.Write(item2.Key);
                     sw.Write(" ");
                     sw.Write(item2.Value);
                    sw.Write(" ");
                }
                sw.WriteLine();
            }
            sw.Close();
            await twitchBot.SendMessage($"Wallets have been manually saved."); 
        }

        static async Task Main(string[] args)
        {
            string password = "";
            string botUsername = "bot_gangster";
            String[] split;
            int value;

            var twitchBot = new Bot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            await twitchBot.JoinChannel("rare_gangster");
            //await twitchBot.SendMessage("zang227", "Hey my bot has started up");
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                if (twitchChatMessage.Message.StartsWith("$raffle"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        if (RaffleStarted)
                        {
                            await twitchBot.SendMessage($"@{twitchChatMessage.Sender} A raffle is already in progress!");
                        }
                        else
                        {
                            split = twitchChatMessage.Message.Split(" ");
                            if (split.Length > 1)
                            {
                                if (Int32.TryParse(split[1], out value))
                                {
                                    entrants.Clear();
                                    pickable.Clear();
                                    RaffleComplete = false;
                                    RaffleStarted = true;
                                    await RaffleTime(value, twitchBot);
                                }
                                else
                                {
                                    await twitchBot.SendMessage($"@{twitchChatMessage.Sender} Invalid length of time for raffle.");
                                }

                            }
                            else
                            {
                                entrants.Clear();
                                pickable.Clear();
                                RaffleComplete = false;
                                RaffleStarted = true;
                                await RaffleTime(0, twitchBot);
                            }
                        }
                    }

                }
                if (twitchChatMessage.Message.StartsWith("$cancel"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        if (RaffleStarted)
                        {
                            Cancel = true;
                        }
                        else
                        {
                            await twitchBot.SendMessage($"@{twitchChatMessage.Sender} A raffle is already in progress!");
                        }
                    }

                }
                if (twitchChatMessage.Message.StartsWith("$reroll"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        if (!RaffleComplete)
                        {
                            if (!RaffleStarted)
                            {
                                await twitchBot.SendMessage($"@{twitchChatMessage.Sender} There is no raffle to reroll!");
                            }
                            else
                                await twitchBot.SendMessage($"@{twitchChatMessage.Sender} You cannot reroll until the raffle has ended!");
                        }
                        else
                        {
                            if (pickable.Count > 0)
                            {
                                var rand = new Random();
                                int winnerIndex;
                                winnerIndex = rand.Next(pickable.Count);
                                winner = pickable[winnerIndex];
                                pickable.RemoveAt(winnerIndex);
                                await twitchBot.SendMessage($"/me The new winner is {winner}");
                            }
                            else
                            {
                                await twitchBot.SendMessage($"/me There are no more entrants to pick from!");
                            }


                        }
                    }

                }
                if (twitchChatMessage.Message.StartsWith("!join"))
                {
                    if (RaffleStarted)
                    {
                        AddEntrant(twitchChatMessage.Sender);
                    }
                }
                if (twitchChatMessage.Message.StartsWith("$who"))
                {
                    if (RaffleComplete)
                    {
                        await twitchBot.SendMessage($"/me The winner of the raffle is {winner}");
                    }
                }
                if (twitchChatMessage.Message.StartsWith("$zangisthebest"))
                {
                    await twitchBot.SendMessage($"Flattery will get you nowhere. ");
                }
                if (twitchChatMessage.Message.StartsWith("$rig"))
                {
                    if (RaffleStarted)
                    {
                        await twitchBot.SendMessage($"Nice try raregaCheeky");
                    }
                }
                if (twitchChatMessage.Message.StartsWith("$test"))
                {
                    await twitchBot.SendMessage($"/announce Testing...");
                }
                if (twitchChatMessage.Message.StartsWith("$myWallet"))
                {
                    walletCheck(twitchChatMessage.Sender.ToLower());
                    await twitchBot.SendMessage("@" + wallets[twitchChatMessage.Sender.ToLower()].getInventory());
                }
                if (twitchChatMessage.Message.StartsWith("$walletAdd"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        var command = twitchChatMessage.Message.Split(" ");
                        var user = "";
                        var item = "";
                        var quant = "";
                        if (command.Length > 2)
                        {
                            user = command[1];
                            user = user.Trim('@');
                            user = user.ToLower();
                            item = itemConvert(command[2]);
                        }
                        if (command.Length > 3)
                        {
                            quant = command[3];
                        }
                        walletCheck(user);
                        if (Int32.TryParse(quant, out value))
                        {
                            await twitchBot.SendMessage(wallets[user].addItem(item, value));
                        }
                        else
                            await twitchBot.SendMessage(wallets[user].addItem(item, 1));

                    }

                }
                if (twitchChatMessage.Message.StartsWith("$walletRemove"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        var command = twitchChatMessage.Message.Split(" ");
                        var user = "";
                        var item = "";
                        var quant = "";
                        if (command.Length > 2)
                        {
                            user = command[1];
                            user = user.Trim('@');
                            user = user.ToLower();
                            item = itemConvert(command[2]);
                        }
                        if (command.Length > 3)
                        {
                            quant = command[3];
                        }
                        walletCheck(user);
                        if (Int32.TryParse(quant, out value))
                        {
                            await twitchBot.SendMessage(wallets[user].removeItem(item, value));
                        }
                        else
                            await twitchBot.SendMessage(wallets[user].removeItem(item, 1));

                    }
                }
                if (twitchChatMessage.Message.StartsWith("$checkWallet"))
                {
                    if (CheckMod(twitchChatMessage.Sender))
                    {
                        var command = twitchChatMessage.Message.Split(" ");
                        if (command.Length > 1)
                        {
                            var user = command[1];
                            user = user.Trim('@');
                            user = user.ToLower();
                            walletCheck(user);
                            await twitchBot.SendMessage(wallets[user].getInventory());
                        }


                    }

                }
                if (twitchChatMessage.Message.StartsWith("$save"))
                {
                    if (CheckAdmin(twitchChatMessage.Sender))
                    {
                        await saveWalletsManual(twitchBot);

                    }
                    else
                        await twitchBot.SendMessage($"You must be a bot admin to use this command.");

                }
                if (twitchChatMessage.Message.StartsWith("$redeem"))
                {
                    var command = twitchChatMessage.Message.Split(" ");
                    var user = twitchChatMessage.Sender;
                    var item = "";
                    var quant = "";
                    if (command.Length > 1)
                    {
                        item = itemConvert(command[1]);
                    }
                    if (command.Length > 2)
                    {
                        quant = command[2];
                    }
                    walletCheck(user);
                    if (Int32.TryParse(quant, out value))
                    {
                        var test = wallets[user].redeemItem(item, value);
                        if (test == 1)
                            await twitchBot.SendMessage($"/announce @rare_gangster {user} has redeemed {value} {item}s from their wallet!");
                        else if (test == -1)
                            await twitchBot.SendMessage("Invalid item name. Please check you're spelling and try again.");
                        else if (test == 0)
                            await twitchBot.SendMessage("You do not have enough to redeem this many!");
                    }
                    else
                    {
                        var test = wallets[user].redeemItem(item, 1);
                        if (test == 1)
                            await twitchBot.SendMessage($"/announce @rare_gangster {user} has redeemed a {item} from their wallet!");
                        else if (test == -1)
                            await twitchBot.SendMessage("Invalid item name. Please check you're spelling and try again.");
                        else if (test == 0)
                            await twitchBot.SendMessage("You do not have enough to redeem this many!");
                    }
                }
                if (twitchChatMessage.Message.StartsWith("$give"))
                {

                    var command = twitchChatMessage.Message.Split(" ");
                    var user = twitchChatMessage.Sender;
                    var toUser = "";
                    var item = "";
                    var quant = "";
                    if (command.Length > 2)
                    {
                        toUser = command[1];
                        toUser = toUser.Trim('@');
                        toUser = toUser.ToLower();
                        item = itemConvert(command[2]);
                    }
                    if (command.Length > 3)
                    {
                        quant = command[3];
                    }
                    walletCheck(user);
                    walletCheck(toUser);
                    if (Int32.TryParse(quant, out value))
                    {
                        var test = wallets[user].redeemItem(item, value);
                        if (test == 1)
                        {
                            await twitchBot.SendMessage($"{user} has given {value} {item}s from their wallet to {toUser}!");
                            await twitchBot.SendMessage(wallets[toUser].addItem(item, value));

                        }
                        else if (test == -1)
                            await twitchBot.SendMessage("Invalid item name. Please check you're spelling and try again.");
                        else if (test == 0)
                            await twitchBot.SendMessage("You do not have enough to give this many!");
                    }
                    else
                    {
                        var test = wallets[user].redeemItem(item, 1);
                        if (test == 1)
                        {
                            await twitchBot.SendMessage($"{user} has given a {item} to {toUser}!");
                            await twitchBot.SendMessage(wallets[toUser].addItem(item, 1));
                        }
                        else if (test == -1)
                            await twitchBot.SendMessage("Invalid item name. Please check you're spelling and try again.");
                        else if (test == 0)
                            await twitchBot.SendMessage("You do not have enough to give this many!");
                    }

                
                }
            };
            loadWallets();
            await saveWallets();
            await Task.Delay(-1);
            
        }
    }
}

public class Wallet
{
    public String name;
    public Dictionary<string, int> wallet = new Dictionary<string, int>();

    public Wallet(String name)
    {
        this.name = name;
        wallet.Add("DeathCard", 0);
        wallet.Add("Soda", 0);
        wallet.Add("TimeOut", 0);
        wallet.Add("PigHat", 0);
        wallet.Add("SOS", 0);
        wallet.Add("KittyTreat", 0);
        wallet.Add("HoofHands", 0);
        wallet.Add("Shot", 0);
        wallet.Add("CC", 0);
        wallet.Add("Onesie", 0);
        wallet.Add("FendiBan", 0);
        wallet.Add("Luigi", 0);
        wallet.Add("Other", 0);


    }

    public String getInventory()
    {
        String start = name + " has the following items in their wallet: ";
        String Swallet = "";
        foreach (var item in wallet)
        {
            if(item.Value > 0)
            {
                Swallet += item.Value + "x" + item.Key + ", ";
            }

        }
        if (Swallet.Equals(""))
        {
            return (name + " has nothing in their wallet!");
        }
        else
            Swallet = Swallet.Remove(Swallet.Length - 2, 2);
        return (start + Swallet);
    }

    public string addItem(String item,int num)
    {
        if (wallet.ContainsKey(item))
        {
            wallet[item] += num;
            return ("Added " + num + " " + item + " to " + name + "'s Wallet!");
        }
        else
            return "Invalid item name. Please check you're spelling and try again.";
        
    }

    public string removeItem(String item, int num)
    {
        if (wallet.ContainsKey(item))
        {
            if (num <= wallet[item])
            {
                wallet[item] -= num;
                return ("Removed " + num + " " + item + " from " + name + "'s Wallet!");
            }
            else
                return "You cannot remove more items than they have in their wallet!";
            
        }
        else
            return "Invalid item name. Please check you're spelling and try again.";
    }

    public int redeemItem(String item, int num)
    {
        if (wallet.ContainsKey(item))
        {
            if (num <= wallet[item])
            {
                wallet[item] -= num;
                return 1;
            }
            else
                return 0;
        }
        else    
            return -1;
    }


    public Dictionary<string, int> getDict()
    {
        return wallet;
    }

}



public class Bot
{
    const string ip = "irc.chat.twitch.tv";
    const int port = 6667;

    private string nick;
    private string password;
    private StreamReader streamReader;
    private StreamWriter streamWriter;
    private TaskCompletionSource<int> connected = new TaskCompletionSource<int>();

    public event TwitchChatEventHandler OnMessage = delegate { };
    public delegate void TwitchChatEventHandler(object sender, TwitchChatMessage e);

    public class TwitchChatMessage : EventArgs
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public string Channel { get; set; }
    }

    public Bot(string nick, string password)
    {
        this.nick = nick;
        this.password = password;
    }

    public async Task Start()
    {
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(ip, port);
        streamReader = new StreamReader(tcpClient.GetStream());
        streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

        await streamWriter.WriteLineAsync($"PASS {password}");
        await streamWriter.WriteLineAsync($"NICK {nick}");
        connected.SetResult(0);

        while (true)
        {
            string line = await streamReader.ReadLineAsync();
            Console.WriteLine(line);

            string[] split = line.Split(" ");
            //PING :tmi.twitch.tv
            //Respond with PONG :tmi.twitch.tv
            if (line.StartsWith("PING"))
            {
                Console.WriteLine("PONG");
                await streamWriter.WriteLineAsync($"PONG {split[1]}");
            }

            if (split.Length > 2 && split[1] == "PRIVMSG")
            {
                //:mytwitchchannel!mytwitchchannel@mytwitchchannel.tmi.twitch.tv 
                // ^^^^^^^^
                //Grab this name here
                int exclamationPointPosition = split[0].IndexOf("!");
                string username = split[0].Substring(1, exclamationPointPosition - 1);
                //Skip the first character, the first colon, then find the next colon
                int secondColonPosition = line.IndexOf(':', 1);//the 1 here is what skips the first character
                string message = line.Substring(secondColonPosition + 1);//Everything past the second colon
                string channel = split[2].TrimStart('#');

                OnMessage(this, new TwitchChatMessage
                {
                    Message = message,
                    Sender = username,
                    Channel = channel
                });
            }
        }
    }

    public async Task SendMessage(string message)

    {
        string channel = "rare_gangster";
        await connected.Task;
        await streamWriter.WriteLineAsync($"PRIVMSG #{channel} :{message}");
    }

    public async Task JoinChannel(string channel)
    {
        await connected.Task;
        await streamWriter.WriteLineAsync($"JOIN #{channel}");
    }
}
