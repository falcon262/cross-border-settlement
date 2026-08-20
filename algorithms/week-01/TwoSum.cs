/*
Given an array of integers nums and an integer target, return the indices i and j such that nums[i] + nums[j] == target and i != j.
You may assume that every input has exactly one pair of indices i and j that satisfy the condition.
Return the answer with the smaller index first.


- My approach: 
- I used a nested loop to iterate through the array and check if the sum of the two numbers is equal to the target.
- If it is, I return the indices of the two numbers.
- If it is not, I continue the loop.
- Time complexity: O(n^2)
- Space complexity: O(1)
- I could solve this unaided but exceeded the timer limit.

*/

public class Solution {
    public int[] TwoSum(int[] nums, int target) {

        for (int i = 0; i < nums.Length; i++) {
            for (int j = i+1; j < nums.Length; j++) {
                if(nums[i] + nums[j] == target)
                {
                    return new int[] {i, j};
                }
            }
        }

        return null;
    }
}