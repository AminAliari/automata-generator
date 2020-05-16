using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace final_project {
    class Simplifier {

        private List<Rule> rules;

        public Simplifier() {
            rules = new List<Rule>();

            print("enter rules of your context free grammer, press enter to simplify your rules.\n\ninput example: S -> abS | abA | abB | * (stands for lambda)\n");
            get();

            if (rules.Count > 0) {
                simplify();
            } else {
                print("no rules to simplify.");
            }
        }

        private void get() {

            int n = 1;
            string input = "";

            while (true) {
                print($"{n}: ", false);
                input = read();

                if (input == "") {
                    clearLine();
                    break;
                } else if (fetch(input)) {
                    n++;
                }
            }
        }

        private void simplify() {

            /////////////// remove repeated rules ///////////////
            rules = rules.Distinct().ToList();

            /////////////// remove lambda productions ///////////////
            List<string> nulls = new List<string>();
            for (int i = rules.Count - 1; i >= 0; i--) {
                if (rules[i].r == "*") {
                    nulls.Add(rules[i].l);
                    rules.Remove(rules[i]);
                }
            }
            nulls = nulls.Distinct().ToList();

            bool isCheck = true;
            while (isCheck) {
                isCheck = false;
                foreach (Rule t in rules) {
                    string tempR = t.r;
                    foreach (string s in nulls) {
                        tempR = tempR.Replace(s, "");
                    }
                    if (tempR.Length == 0) {
                        int oldCount = nulls.Count;
                        nulls.Add(t.l);
                        nulls = nulls.Distinct().ToList();
                        isCheck = oldCount != nulls.Count;
                    }
                }
            }
            nulls = nulls.Distinct().ToList();

            int oldRulesCount = rules.Count;

            for (int index = 0; index < oldRulesCount; index++) {
                int nullsCount = 0;

                foreach (char c in rules[index].r) {
                    if (nulls.Contains(c.ToString())) {
                        nullsCount++;
                    }
                }

                if (nullsCount > 0) {
                    for (int counter = 0; counter < (int)Math.Pow(2, nullsCount); counter++) {
                        string ruleCopy = rules[index].r;
                        string comb = Convert.ToString(counter, 2);
                        int zeroCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(comb.Length) / nullsCount)) * nullsCount;
                        comb = comb.PadLeft(zeroCount, '0');
                        int i = comb.Length - 1, j = ruleCopy.Length - 1;

                        while (i >= 0 && j >= 0) {
                            if (nulls.Contains(ruleCopy[j].ToString())) {
                                if (comb[i] == '0') {
                                    ruleCopy = ruleCopy.Remove(j, 1);
                                }
                                i--;
                            }
                            j--;
                        }

                        if (ruleCopy != "") {
                            rules.Add(new Rule(rules[index].l, ruleCopy));
                        }
                    }
                }
            }
            rules = rules.Distinct().ToList();

            /////////////// remove unit productions ///////////////
            List<Rule> newRules = new List<Rule>(), unitRules = new List<Rule>();

            foreach (Rule rule in rules) {
                if (rule.r.Length == 1 && rule.r == rule.r.ToUpper()) {
                    unitRules.Add(rule);
                } else {
                    newRules.Add(rule);
                }
            }

            foreach (Rule rule in unitRules) {
                List<string> rights = getDerivations(rule.r, new List<string> { rule.r });
                foreach (string right in rights) {
                    newRules.Add(new Rule(rule.l, right));
                }
            }

            for (int i = newRules.Count - 1; i >= 0; i--) {
                if (newRules[i].r.Length == 1 && newRules[i].r == newRules[i].r.ToUpper()) {
                    newRules.Remove(newRules[i]);
                }
            }
            rules = newRules.Distinct().ToList();

            /////////////// remove useless productions ///////////////
            List<string> w = new List<string>(), terminals = new List<string>();

            foreach (Rule rule in rules) {
                foreach (char c in rule.r) {
                    if (c.ToString() == c.ToString().ToLower()) {
                        w.Add(rule.l);
                        terminals.Add(c.ToString());
                    }
                }
            }
            terminals = terminals.Distinct().ToList();
            w = w.Distinct().ToList();

            isCheck = true;

            while (isCheck) {
                foreach (Rule rule in rules) {
                    for (int v = w.Count - 1; v >= 0; v--) {
                        if (rule.r.Contains(w[v])) {
                            int wOldCount = w.Count;
                            w.Add(rule.l);
                            w = w.Distinct().ToList();
                            isCheck = wOldCount != w.Count;
                        }
                    }
                }
            }
            w = w.Distinct().ToList();

            for (int i = rules.Count - 1; i >= 0; i--) {
                bool isUseless = false;
                foreach (char v in rules[i].r) {
                    string s = v.ToString();
                    if (s == s.ToUpper() && !w.Contains(s)) {
                        isUseless = true;
                        break;
                    }
                }
                if (isUseless) {
                    rules.Remove(rules[i]);
                }
            }
            rules = rules.Distinct().ToList();

            if (rules.Count > 0) {

                List<string> y = new List<string>();
                y.Add(rules[0].l);

                isCheck = true;

                while (isCheck) {
                    int yOldCount = y.Count;
                    for (int left = y.Count - 1; left >= 0; left--) {
                        foreach (Rule rule in rules) {
                            if (y[left] == rule.l) {

                                foreach (char c in rule.r) {
                                    string s = c.ToString();
                                    if (s == s.ToUpper()) {
                                        y.Add(s);
                                    }
                                }
                            }
                        }
                    }
                    y = y.Distinct().ToList();
                    isCheck = yOldCount != y.Count;
                }

                newRules = new List<Rule>();

                foreach (string left in y) {
                    foreach (Rule rule in rules) {
                        if (left == rule.l) {
                            newRules.Add(rule);
                        }
                    }
                }
                rules = newRules.Distinct().ToList();

                printList(rules);
            }
        }

        private List<string> getDerivations(string startNode, List<string> seenNodes) {
            List<string> rights = new List<string>();

            foreach (Rule rule in rules) {
                if (startNode == rule.l) {
                    if (rule.r == rule.r.ToUpper() && rule.r.Length == 1 && !seenNodes.Contains(rule.r)) {
                        seenNodes.Add(rule.r);
                        List<string> newRights = getDerivations(rule.r, seenNodes);
                        foreach (string right in newRights) {
                            rights.Add(right);
                        }
                    } else {
                        rights.Add(rule.r);
                    }
                }
            }

            return rights.Distinct().ToList();
        }

        private bool fetch(string input) {

            List<string> errors = new List<string>();
            List<string> tempRights = new List<string>();
            string left = "";

            if (input.Length > 0) {
                string[] lr = input.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

                if (lr.Length == 2) {
                    lr[0] = lr[0].Trim();

                    if (lr[0].Length == 1) {
                        if (lr[0] == lr[0].ToUpper()) {
                            left = lr[0];
                        } else {
                            errors.Add("not a context free grammer");
                        }
                    } else {
                        errors.Add("not a context free grammer");
                    }

                    lr[1] = lr[1].Trim();
                    string[] rParts = lr[1].Split('|');

                    if (rParts.Length > 0) {
                        if (rParts.Contains("")) {
                            errors.Add("extra (|) sign used. (empty rule)");
                        } else {
                            foreach (string p in rParts) {
                                string tempP = p.Trim();
                                if ((!Regex.IsMatch(tempP, @"^[a-zA-Z]+$")) && tempP != "*") {
                                    errors.Add("rules parts must only contain alphabet characters");
                                    break;
                                }
                                tempRights.Add(tempP);
                            }
                        }
                    } else {
                        errors.Add("empty right part of the rule");
                    }

                } else {
                    errors.Add("incomplete left/right part of the rule");
                }
            } else {
                errors.Add("empty rule");
            }

            if (errors.Count > 0) {
                print("errors of entered rule:");
                printList(errors);
                return false;
            }

            foreach (string r in tempRights) {
                rules.Add(new Rule(left, r));
            }
            return true;
        }

        public static void printList<T>(List<T> list) {
            foreach (T item in list) {
                print(item);
            }
        }

        public static string read() {
            return Console.ReadLine();
        }

        public static void print(Object o) {
            Console.WriteLine(o.ToString());
        }

        public static void print(Object o, bool line) {
            Console.Write(o.ToString());
        }

        public static void clearLine() {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
            print("");
        }

        class Rule {
            public string l, r;

            public Rule(string l, string r) {
                this.l = l;
                this.r = r;
            }

            public override bool Equals(Object obj) {
                Rule other = (Rule)obj;
                return l == other.l && r == other.r;
            }

            public override int GetHashCode() {
                return l.GetHashCode() ^ r.GetHashCode();
            }

            public override string ToString() {
                return $"{l} -> {r}";
            }
        }
    }
}
