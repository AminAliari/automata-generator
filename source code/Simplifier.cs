using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace final_project {
    class Simplifier {

        private List<Rule> rules;
        private string startSymbol;

        public Simplifier() {
            rules = new List<Rule>();
            startSymbol = "";

            print("enter rules of your context free grammer, press enter to simplify your rules.\n\ninput example: S -> a b S | a b A | a b B | * (stands for lambda)\n");
            get();

            if (rules.Count > 0) {
                simplify();
            } else {
                print("no rules to simplify.");
            }
        }

        private void get() {

            int n = 1;

            while (true) {
                print($"{n}: ", false);
                string input = read();

                if (input == "") {
                    clearLine();
                    break;
                }

                if (fetch(input)) {
                    n++;
                }
            }
        }

        private void simplify() {

            rules = rules.Distinct().ToList();
            HashSet<string> nonTerminals = getNonTerminals(rules);

            HashSet<string> nullableNonTerminals = getNullableNonTerminals(rules, nonTerminals);
            rules = rules.Where(rule => !rule.IsLambda).ToList();

            List<Rule> lambdaExpandedRules = new List<Rule>(rules);
            foreach (Rule rule in rules) {
                List<int> nullablePositions = new List<int>();
                for (int i = 0; i < rule.r.Count; i++) {
                    if (nullableNonTerminals.Contains(rule.r[i])) {
                        nullablePositions.Add(i);
                    }
                }

                int nullableCount = nullablePositions.Count;
                for (int mask = 1; mask < (1 << nullableCount); mask++) {
                    HashSet<int> indicesToRemove = new HashSet<int>();
                    for (int bit = 0; bit < nullableCount; bit++) {
                        if ((mask & (1 << bit)) != 0) {
                            indicesToRemove.Add(nullablePositions[bit]);
                        }
                    }

                    List<string> newRight = new List<string>();
                    for (int i = 0; i < rule.r.Count; i++) {
                        if (!indicesToRemove.Contains(i)) {
                            newRight.Add(rule.r[i]);
                        }
                    }

                    if (newRight.Count > 0) {
                        lambdaExpandedRules.Add(new Rule(rule.l, newRight));
                    }
                }
            }
            rules = lambdaExpandedRules.Distinct().ToList();

            nonTerminals = getNonTerminals(rules);

            List<Rule> nonUnitRules = rules.Where(rule => !isUnitProduction(rule, nonTerminals)).ToList();
            List<Rule> withoutUnitProductions = new List<Rule>(nonUnitRules);

            foreach (string source in nonTerminals) {
                HashSet<string> reachableByUnits = getUnitReachableNonTerminals(source, rules, nonTerminals);
                foreach (string target in reachableByUnits) {
                    foreach (Rule targetRule in nonUnitRules) {
                        if (targetRule.l == target) {
                            withoutUnitProductions.Add(new Rule(source, targetRule.r));
                        }
                    }
                }
            }

            rules = withoutUnitProductions.Distinct().ToList();

            nonTerminals = getNonTerminals(rules);
            HashSet<string> generatingNonTerminals = getGeneratingNonTerminals(rules, nonTerminals);

            rules = rules
                .Where(rule => isProductionGenerating(rule, generatingNonTerminals, nonTerminals))
                .Distinct()
                .ToList();

            if (rules.Count == 0) {
                return;
            }

            nonTerminals = getNonTerminals(rules);
            startSymbol = determineStartSymbol(nonTerminals, rules, startSymbol);

            HashSet<string> reachableNonTerminals = getReachableNonTerminals(startSymbol, rules, nonTerminals);
            rules = rules.Where(rule => reachableNonTerminals.Contains(rule.l)).Distinct().ToList();

            printList(rules);
        }

        private HashSet<string> getNonTerminals(List<Rule> sourceRules) {
            return new HashSet<string>(sourceRules.Select(rule => rule.l));
        }

        private HashSet<string> getNullableNonTerminals(List<Rule> sourceRules, HashSet<string> nonTerminals) {
            HashSet<string> nullable = new HashSet<string>(sourceRules.Where(rule => rule.IsLambda).Select(rule => rule.l));

            bool changed = true;
            while (changed) {
                changed = false;

                foreach (Rule rule in sourceRules) {
                    if (rule.IsLambda) {
                        continue;
                    }

                    bool allNullable = rule.r.All(symbol => nonTerminals.Contains(symbol) && nullable.Contains(symbol));
                    if (allNullable && nullable.Add(rule.l)) {
                        changed = true;
                    }
                }
            }

            return nullable;
        }

        private bool isUnitProduction(Rule rule, HashSet<string> nonTerminals) {
            return rule.r.Count == 1 && nonTerminals.Contains(rule.r[0]);
        }

        private HashSet<string> getUnitReachableNonTerminals(string source, List<Rule> sourceRules, HashSet<string> nonTerminals) {
            HashSet<string> visited = new HashSet<string> { source };
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(source);

            while (queue.Count > 0) {
                string current = queue.Dequeue();

                foreach (Rule rule in sourceRules) {
                    if (rule.l != current || !isUnitProduction(rule, nonTerminals)) {
                        continue;
                    }

                    string target = rule.r[0];
                    if (visited.Add(target)) {
                        queue.Enqueue(target);
                    }
                }
            }

            return visited;
        }

        private HashSet<string> getGeneratingNonTerminals(List<Rule> sourceRules, HashSet<string> nonTerminals) {
            HashSet<string> generating = new HashSet<string>();

            bool changed = true;
            while (changed) {
                changed = false;

                foreach (Rule rule in sourceRules) {
                    bool rhsGenerating = true;

                    foreach (string symbol in rule.r) {
                        if (nonTerminals.Contains(symbol) && !generating.Contains(symbol)) {
                            rhsGenerating = false;
                            break;
                        }
                    }

                    if (rhsGenerating && generating.Add(rule.l)) {
                        changed = true;
                    }
                }
            }

            return generating;
        }

        private bool isProductionGenerating(Rule rule, HashSet<string> generatingNonTerminals, HashSet<string> nonTerminals) {
            foreach (string symbol in rule.r) {
                if (nonTerminals.Contains(symbol) && !generatingNonTerminals.Contains(symbol)) {
                    return false;
                }
            }

            return true;
        }

        private string determineStartSymbol(HashSet<string> nonTerminals, List<Rule> sourceRules, string preferredStartSymbol) {
            if (!string.IsNullOrWhiteSpace(preferredStartSymbol) && nonTerminals.Contains(preferredStartSymbol)) {
                return preferredStartSymbol;
            }

            HashSet<string> referencedNonTerminals = new HashSet<string>(
                sourceRules
                    .SelectMany(rule => rule.r)
                    .Where(symbol => nonTerminals.Contains(symbol))
            );

            List<string> rootCandidates = nonTerminals
                .Where(nonTerminal => !referencedNonTerminals.Contains(nonTerminal))
                .OrderBy(nonTerminal => nonTerminal)
                .ToList();

            if (rootCandidates.Count == 1) {
                return rootCandidates[0];
            }

            if (nonTerminals.Contains("S")) {
                return "S";
            }

            if (rootCandidates.Count > 0) {
                return rootCandidates[0];
            }

            return nonTerminals.OrderBy(nonTerminal => nonTerminal).First();
        }

        private HashSet<string> getReachableNonTerminals(string start, List<Rule> sourceRules, HashSet<string> nonTerminals) {
            HashSet<string> reachable = new HashSet<string>();

            if (string.IsNullOrWhiteSpace(start) || !nonTerminals.Contains(start)) {
                return reachable;
            }

            Queue<string> queue = new Queue<string>();
            queue.Enqueue(start);
            reachable.Add(start);

            while (queue.Count > 0) {
                string current = queue.Dequeue();

                foreach (Rule rule in sourceRules) {
                    if (rule.l != current) {
                        continue;
                    }

                    foreach (string symbol in rule.r) {
                        if (nonTerminals.Contains(symbol) && reachable.Add(symbol)) {
                            queue.Enqueue(symbol);
                        }
                    }
                }
            }

            return reachable;
        }

        private bool fetch(string input) {

            List<string> errors = new List<string>();
            List<Rule> parsedRules = new List<Rule>();
            string left = "";

            if (!string.IsNullOrWhiteSpace(input)) {
                string[] lr = input.Split(new[] { "->" }, StringSplitOptions.None);

                if (lr.Length == 2) {
                    left = lr[0].Trim();

                    if (!isValidSymbol(left)) {
                        errors.Add("left side must be a single non-empty symbol without spaces");
                    }

                    string rightPart = lr[1].Trim();
                    string[] alternatives = rightPart.Split('|');

                    if (alternatives.Length == 0) {
                        errors.Add("empty right part of the rule");
                    } else {
                        foreach (string part in alternatives) {
                            string trimmedPart = part.Trim();
                            if (trimmedPart == "") {
                                errors.Add("extra (|) sign used. (empty rule)");
                                break;
                            }

                            if (trimmedPart == "*") {
                                parsedRules.Add(new Rule(left, new List<string>()));
                                continue;
                            }

                            List<string> tokens = tokenizeRightPart(trimmedPart);
                            if (tokens.Count == 0) {
                                errors.Add("right side must contain at least one symbol or * for lambda");
                                break;
                            }

                            if (tokens.Any(token => !isValidSymbol(token) || token == "*")) {
                                errors.Add("right side contains invalid symbols");
                                break;
                            }

                            parsedRules.Add(new Rule(left, tokens));
                        }
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

            rules.AddRange(parsedRules);
            if (string.IsNullOrWhiteSpace(startSymbol)) {
                startSymbol = left;
            }

            return true;
        }

        private List<string> tokenizeRightPart(string rightPart) {
            if (Regex.IsMatch(rightPart, @"\s")) {
                return rightPart
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            return rightPart.Select(c => c.ToString()).ToList();
        }

        private bool isValidSymbol(string symbol) {
            return !string.IsNullOrWhiteSpace(symbol)
                && !symbol.Contains("->")
                && !symbol.Contains("|")
                && !Regex.IsMatch(symbol, @"\s");
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
            public string l;
            public List<string> r;

            public Rule(string l, List<string> r) {
                this.l = l;
                this.r = new List<string>(r);
            }

            public bool IsLambda {
                get { return r.Count == 0; }
            }

            public override bool Equals(Object obj) {
                Rule other = obj as Rule;
                if (other == null) {
                    return false;
                }

                return l == other.l && r.SequenceEqual(other.r);
            }

            public override int GetHashCode() {
                unchecked {
                    int hash = 17;
                    hash = hash * 23 + l.GetHashCode();
                    foreach (string symbol in r) {
                        hash = hash * 23 + symbol.GetHashCode();
                    }
                    return hash;
                }
            }

            public override string ToString() {
                return $"{l} -> {(IsLambda ? "*" : string.Join(" ", r))}";
            }
        }
    }
}
