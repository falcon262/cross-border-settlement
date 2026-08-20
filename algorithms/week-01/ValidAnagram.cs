/*
Given two strings s and t, return true if t is an anagram of s, and false otherwise.
- My approach: 
- I used an array to store the number of occurrences of each character in the two strings. I then compared the array to see if the number of occurrences of each character is the same. If it is, I return true. If it is not, I return false.
- Time complexity: O(n)
- Space complexity: O(1)
- I could not solve this unaided and inside the timer limit.(log this in the failures.md file.)
*/

public class Solution {
    public bool IsAnagram(string s, string t) {

        int[] alphabet = new int[26];
        if(s.Length != t.Length){
            return false;
        }
        else
        {
            for (int i = 0; i < s.Length; i++) {
                alphabet[s[i] - 'a']++;
                alphabet[t[i] - 'a']--;
            }
            foreach (var item in alphabet) {
                if(item != 0)
                return false;
            }
        }
        

        return true;
    }
}

