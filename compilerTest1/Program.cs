using System;
using System.Collections.Generic;
using System.Linq;

namespace compilerTest1
{
    class Program
    {
        static void Main(string[] args)
        {
            //LL1
            //class call
            int flag = 1;
            do
            {
                var bg = new BarresiGhanun();
                int flag2 = 1;
                do
                {
                    //get rules
                    Console.WriteLine("Enter the Rule using left->state/state method.");
                    string rule = Console.ReadLine();
                    if (rule.Trim().Length == 0)
                    {
                        Console.WriteLine("Input not right.");
                        break;
                    }
                    bg.Rules.Add(bg.CreateTextGhanun(rule));
                    Console.WriteLine("Enter 1 to add Rule or 0 to stop.");
                    flag2 = Convert.ToInt32(Console.ReadLine());
                } while (flag2 == 1);
                //check if the rule is LL1 or not
                bg.CheckEverything();
                Console.WriteLine("Enter 1 to test another grammar or 0 to stop.");
                flag = Convert.ToInt32(Console.ReadLine());
                if (flag == 0)
                {
                    Console.WriteLine("Exited.");
                    break;
                }
                else
                {
                    Console.Clear();
                    bg.Rules.Clear();
                    bg.TokenMap.Clear();
                    BarresiGhanun.firsts.Clear();
                    BarresiGhanun.follows.Clear();
                    BarresiGhanun.lefts.Clear();
                    BarresiGhanun.terminals.Clear();
                }
            } while (flag == 1);
        }
    }
    //interface for rule name
    public interface IGhanun
    {
        string Name { get; }
    }
    //class token with two statics
    public class Token : IGhanun
    {
        public string Name { get; }

        public Token(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static Token Edoll = new Token("$");
        public static Token Empty = new Token("?");
    }
    //in this class there are rule and its front phrases
    public class Ghanun : IGhanun
    {
        public string Name { get; }

        public IEnumerable<IEnumerable<IGhanun>> Rights { get; }

        public Ghanun(string name, IEnumerable<IEnumerable<IGhanun>> rights)
        {
            Name = name;
            Rights = rights;
        }
        //for showing the rule
        public override string ToString()
        {
            return $"{Name}->" + string.Join("/", Rights.Select(definition =>
                string.Join("", definition.Select(rule => rule.Name))
                ));
        }
    }
    //in this classe, being LL1 is checked
    public class BarresiGhanun
    {
        //variable definition
        public List<Ghanun> Rules = new List<Ghanun>();
        public Dictionary<string, Token> TokenMap = new Dictionary<string, Token>();
        public static List<string> lefts = new List<string>();
        public static List<string> terminals = new List<string>();
        public static List<List<Token>> firsts = new List<List<Token>>();
        public static List<List<Token>> follows = new List<List<Token>>();
        //token
        private Token GetToken(string tokenName)
        {
            if (tokenName == "$")
                return Token.Edoll;
            if (tokenName == "?")
                return Token.Empty;
            if (!TokenMap.ContainsKey(tokenName))
                TokenMap.Add(tokenName, new Token(tokenName));
            return TokenMap[tokenName];
        }
        //variable or terminal
        public bool IsCapital(string s)
        {
            bool b = false;
            for (var ch = 'A'; ch <= 'Z'; ch++)
            {
                if (s == ch.ToString())
                {
                    b = true;
                }
            }
            return b;
        }
        //making terminal list
        public void TerminalList(string right)
        {
            for (int i = 0; i < right.Length; i++)
            {
                if (right[i] != '/' && right[i] != '?' && terminals.Contains(right[i].ToString()) == false)
                {
                    if (IsCapital(right[i].ToString()) == false)
                    {
                        terminals.Add(right[i].ToString());
                    }
                }
            }
        }
        //make rules
        public Ghanun CreateTextGhanun(string text)
        {
            var segments = text.Split(new[] { "->" }, StringSplitOptions.None);
            var name = segments[0];
            if (lefts.Contains(name) == false)
            {
                lefts.Add(name);
            }
            var right = segments[1];
            TerminalList(right);
            var ruleRights = right
                .Split('/')
                .Select(ruleSequenceText =>
                {
                    return ruleSequenceText.Select(token =>
                    {
                        return char.IsUpper(token) ?
                            Rules.First(x => x.Name == token.ToString()) :
                            (IGhanun)GetToken(token.ToString());
                    });
                });
            return new Ghanun(name, ruleRights);
        }
        //make first list
        public IEnumerable<Token> First(IEnumerable<IEnumerable<IGhanun>> rights)
        {
            return rights.SelectMany(ruleSequence =>
            {
                var tokens = new List<Token>();
                var hasEmpty = true;
                foreach (var item in ruleSequence)
                {
                    if (item is Token token)
                    {
                        if (token != Token.Empty)
                        {
                            hasEmpty = false;
                            tokens.Add(token);
                            break;
                        }
                    }
                    else if (item is Ghanun gh)
                    {
                        var ruleFirsts = First(gh.Rights);
                        if (!ruleFirsts.Contains(Token.Empty))
                        {
                            hasEmpty = false;
                            tokens.AddRange(ruleFirsts);
                            break;
                        }
                        else
                        {
                            tokens.AddRange(ruleFirsts.Where(x => x != Token.Empty));
                        }
                    }
                }
                if (hasEmpty) tokens.Add(Token.Empty);
                return tokens;
            })
            .Distinct();
        }
        //make follow list
        public IEnumerable<Token> Follow(Ghanun ghanun)
        {
            return Rules
                .SelectMany(knownRule =>
                {
                    return knownRule.Rights
                        .Where(ruleSequence => ruleSequence.Contains(ghanun))
                        .SelectMany(relatedSequence =>
                        {
                            var tokens = new List<Token>();
                            do
                            {
                                relatedSequence = relatedSequence
                                    .SkipWhile(x => x != ghanun)
                                    .Skip(1);

                                if (relatedSequence.Any())
                                {
                                    var firsts = First(new[] { relatedSequence });
                                    tokens.AddRange(firsts.Where(x => x != Token.Empty));
                                    if (firsts.Contains(Token.Empty))
                                    {
                                        tokens.AddRange(Follow(knownRule));
                                    }
                                }
                                else if (knownRule != ghanun)
                                {
                                    tokens.AddRange(Follow(knownRule));
                                }
                            } while (relatedSequence.Contains(ghanun));
                            return tokens;
                        });
                })
                .DefaultIfEmpty(Token.Edoll)
                .Distinct();
        }
        //checking grammar, first, follow and parse
        public void CheckEverything()
        {
            foreach (var item in lefts)
            {
                var lift = new List<Token>();
                var lifw = new List<Token>();
                foreach (Ghanun ghanun in Rules)
                {
                    if (ghanun.Name == item)
                    {
                        lift.AddRange(First(ghanun.Rights).ToList());
                        lifw.AddRange(Follow(ghanun).ToList());
                    }
                }
                firsts.Add(lift.Distinct().ToList());
                follows.Add(lifw.Distinct().ToList());
            }
            ShowGhanun();
            Console.WriteLine();
            ShowFirstFollow();
            var parse = MakeParse();
            Console.WriteLine();
            ShowParse(parse);
            var b = IfLL1(parse);
            if (b == true)
            {
                Console.WriteLine("Is LL1.");
            }
            else
            {
                Console.WriteLine("Is not LL1.");
            }
        }
        //showing grammar
        public void ShowGhanun()
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                Console.WriteLine(i + ") " + Rules[i].ToString());
            }
        }
        //first and follow table
        public void ShowFirstFollow()
        {
            for (int i = 0; i < firsts.Count; i++)
            {
                Console.WriteLine(Rules[i].Name + "|firsts: " + string.Join(" ", firsts[i]) + " |follows: " + string.Join(" ", follows[i]));
            }
        }
        //making parse
        public string[,] MakeParse()
        {
            terminals.Add("$");
            string[,] parse = new string[lefts.Count, terminals.Count];
            for (int i = 0; i < lefts.Count; i++)
            {
                for (int j = 0; j < terminals.Count; j++)
                {
                    if (firsts[i].Select(s => s.Name).Contains(terminals[j]))
                    {
                        parse[i, j] += i.ToString();
                    }
                    if (follows[i].Select(s => s.Name).Contains(terminals[j]) && firsts[i].Contains(Token.Empty))
                    {
                        if (parse[i, j] != null)
                        {
                            parse[i, j] += "/" + i.ToString();
                        }
                        else
                        {
                            parse[i, j] += i.ToString();
                        }
                    }
                }
            }
            return parse;
        }
        //check if it is LL1 or not
        public bool IfLL1(string[,] parse)
        {
            bool b = true;
            foreach (var item in parse)
            {
                if (item!=null && item.Length > 1 && item.Contains("/"))
                {
                    b = false;
                }
            }
            return b;
        }
        //showing parse table
        public void ShowParse(string[,] parse)
        {
            Console.Write("{0, -5}", "");
            for (int i = 0; i < terminals.Count; i++)
            {
                Console.Write("{0, -5}", terminals[i] + "|");
            }
            Console.WriteLine();
            for (int i = 0; i < lefts.Count; i++)
            {
                Console.Write("{0, -5}", lefts[i] + "|");
                for (int j = 0; j < terminals.Count; j++)
                {
                    Console.Write("{0, -5}", parse[i, j] + "|");
                }
                Console.WriteLine();
            }
        }

    }
}