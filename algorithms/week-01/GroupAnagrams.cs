/*
Given an array of strings strs, group the anagrams together. You can return the answer in any order.
An Anagram is a word or phrase formed by rearranging the letters of a different word or phrase, typically using all the original letters exactly once.

- My approach: 
- I used a dictionary to store the anagrams.
- I used a nested loop to iterate through the array and check if the letters of the word are the same.
- If they are, I add the word to the dictionary.
- If they are not, I continue the loop.
- Time complexity: O(n * m) where n is the number of words in the array and m is the length of the longest word.
- Space complexity: O(n * m) where n is the number of words in the array and m is the length of the longest word.
- I could not solve this unaided and inside the timer limit.(log this in the failures.md file.)
*/

public class Solution {
    public List<List<string>> GroupAnagrams(string[] strs) {
        var res = new Dictionary<string, List<string>>();
        foreach (var s in strs) {
            int[] count = new int[26];
            foreach (char c in s) {
                count[c - 'a']++;
            }
            string key = string.Join(",", count);
            if (!res.ContainsKey(key)) {
                res[key] = new List<string>();
            }
            res[key].Add(s);
        }
        return res.Values.ToList<List<string>>();
    }
}
