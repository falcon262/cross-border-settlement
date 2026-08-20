/*
Given an integer array nums, return true if any value appears at least twice in the array, and return false if every element is distinct.
- My approach: I had a check variable to store if a duplicate is found. I then used a HashSet to store the numbers. If the number is not in the HashSet, I add it to the HashSet. If the number is in the HashSet, I set the check variable to true and return it.
- Time complexity: O(n)
- Space complexity: O(n)
- I solved this unaided and inside the timer limit.
*/

public class Solution {
    public bool hasDuplicate(int[] nums) {
        
        HashSet<int> set = new HashSet<int>();
        bool isDuplicate = false;

        for (int i = 0; i < nums.Length; i++) {
            if(!set.Add(nums[i]))
            {
                isDuplicate = true;
            }
        }

        return isDuplicate;
        
    }
}