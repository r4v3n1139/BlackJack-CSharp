using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using BlackJack_2._0;

namespace BlackJack_2._0
{
    class Program
    {
        private static Deck deck = new Deck();
        private static Player player = new Player();


        private enum RoundResult
        {
            PUSH,
            PLAYER_WIN,
            PLAYER_BUST,
            PLAYER_BLACKJACK,
            DEALER_WIN,
            SURRENDER,
            INVALID_BET
        }

        /// <summary>
        /// Initialize Deck, deal the player and dealer hands, and display them.
        /// </summary>
        static void InitializeHands()
        {
            deck.Initialize();

            player.Hand = deck.DealHand();
            Dealer.HiddenCards = deck.DealHand();
            Dealer.RevealedCards = new List<Card>();

            // If hand contains two aces, make one Hard.
            if (player.Hand[0].Face == Face.Ace && player.Hand[1].Face == Face.Ace)
            {
                player.Hand[1].Value = 1;
            }

            if (Dealer.HiddenCards[0].Face == Face.Ace && Dealer.HiddenCards[1].Face == Face.Ace)
            {
                Dealer.HiddenCards[1].Value = 1;
            }

            Dealer.RevealCard();

            player.WriteHand();
            Dealer.WriteHand();
        }

        /// <summary>
        /// Handles everything for the round.
        /// </summary>
        static void StartRound()
        {
            Console.Clear();

            if (!TakeBet())
            {
                EndRound(RoundResult.INVALID_BET);
                return;
            }
            Console.Clear();

            InitializeHands();
            TakeActions();

            Dealer.RevealCard();

            Console.Clear();
            player.WriteHand();
            Dealer.WriteHand();

            player.HandsCompleted++;

            if (player.Hand.Count == 0)
            {
                EndRound(RoundResult.SURRENDER);
                return;
            }
            else if (player.GetHandValue() > 21)
            {
                EndRound(RoundResult.PLAYER_BUST);
                return;
            }

            while (Dealer.GetHandValue() <= 16)
            {
                Thread.Sleep(1000);
                Dealer.RevealedCards.Add(deck.DrawCard());

                Console.Clear();
                player.WriteHand();
                Dealer.WriteHand();
            }


            if (player.GetHandValue() > Dealer.GetHandValue())
            {
                player.Wins++;
                if (Casino.IsHandBlackjack(player.Hand))
                {
                    EndRound(RoundResult.PLAYER_BLACKJACK);
                }
                else
                {
                    EndRound(RoundResult.PLAYER_WIN);
                }
            }
            else if (Dealer.GetHandValue() > 21)
            {
                player.Wins++;
                EndRound(RoundResult.PLAYER_WIN);
            }
            else if (Dealer.GetHandValue() > player.GetHandValue())
            {
                EndRound(RoundResult.DEALER_WIN);
            }
            else
            {
                EndRound(RoundResult.PUSH);
            }

        }

        /// <summary>
        /// Ask user for action and perform that action until they stay, double, or bust.
        /// </summary>
        static void TakeActions()
        {
            string action;
            do
            {
                Console.Clear();
                player.WriteHand();
                Dealer.WriteHand();

                Console.Write("Enter Action (? for help): ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                action = Console.ReadLine();
                Casino.ResetColor();

                switch (action.ToUpper())
                {
                    case "HIT":
                        player.Hand.Add(deck.DrawCard());
                        break;
                    case "STAY":
                        break;
                    case "SURRENDER":
                        player.Hand.Clear();
                        break;
                    case "DOUBLE":
                        if (player.Chips <= player.Bet)
                        {
                            player.AddBet(player.Chips);
                        }
                        else
                        {
                            player.AddBet(player.Bet);
                        }
                        player.Hand.Add(deck.DrawCard());
                        break;
                    default:
                        Console.WriteLine("Valid Moves:");
                        Console.WriteLine("Hit, Stay, Surrender, Double");
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                        break;
                }

                if (player.GetHandValue() > 21)
                {
                    foreach (Card card in player.Hand)
                    {
                        if (card.Value == 11) // Only a soft ace can have a value of 11
                        {
                            card.Value = 1;
                            break;
                        }
                    }
                }
            } while (!action.ToUpper().Equals("STAY") && !action.ToUpper().Equals("DOUBLE")
                && !action.ToUpper().Equals("SURRENDER") && player.GetHandValue() <= 21);
        }

        /// <summary>
        /// Take player's bet
        /// </summary>
        /// <returns>Was the bet valid</returns>
        static bool TakeBet()
        {
            Console.Write("Current Chip Count: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(player.Chips);
            Casino.ResetColor();

            Console.Write("Minimum Bet: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Casino.MinimumBet);
            Casino.ResetColor();

            Console.Write("Enter bet to begin hand " + player.HandsCompleted + ": ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            string s = Console.ReadLine();
            Casino.ResetColor();

            if (Int32.TryParse(s, out int bet) && bet >= Casino.MinimumBet && player.Chips >= bet)
            {
                player.AddBet(bet);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Perform action based on result of round and start next round.
        /// </summary>
        /// <param name="result">The result of the round</param>
        static void EndRound(RoundResult result)
        {
            switch (result)
            {
                case RoundResult.PUSH:
                    player.ReturnBet();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Player and Dealer Push.");
                    break;
                case RoundResult.PLAYER_WIN:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Player Wins " + player.WinBet(false) + " chips");
                    break;
                case RoundResult.PLAYER_BUST:
                    player.ClearBet();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Player Busts");
                    break;
                case RoundResult.PLAYER_BLACKJACK:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Player Wins " + player.WinBet(true) + " chips with Blackjack.");
                    break;
                case RoundResult.DEALER_WIN:
                    player.ClearBet();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Dealer Wins.");
                    break;
                case RoundResult.SURRENDER:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Player Surrenders " + (player.Bet / 2) + " chips");
                    player.Chips += player.Bet / 2;
                    player.ClearBet();
                    break;
                case RoundResult.INVALID_BET:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid Bet.");
                    break;
            }

            if (player.Chips <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine();
                Console.WriteLine("You ran out of Chips after " + (player.HandsCompleted - 1) + " rounds.");
                Console.WriteLine("500 Chips will be added and your statistics have been reset.");

                player = new Player();
            }

            Casino.ResetColor();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            StartRound();
        }

        static void Main(string[] args)
        {
            // Console cannot render unicode characters without this line
            Console.OutputEncoding = Encoding.UTF8;

            Casino.ResetColor();
            Console.Title = "♠♥♣♦ Blackjack";

            Console.WriteLine("♠♥♣♦ Welcome to Blackjack v" + Casino.GetVersionCode());
            Console.WriteLine("Press any key to play.");
            Console.ReadKey();
            StartRound();
        }
    }
}
