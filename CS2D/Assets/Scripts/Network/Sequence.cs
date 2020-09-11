using UnityEngine;
using System.Collections;

public class Sequence {

	private int minValue;
	private int maxValue;
	private int currentValue;

	public Sequence(int maxValue) {
		this.minValue = 0;
		this.maxValue = maxValue;
		this.currentValue = minValue;
	}

	public Sequence(int minValue, int maxValue) {
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.currentValue = minValue;
	}

	public int Get() {
		return currentValue;
	}

	public void Set(int value) {
		this.currentValue = value;
	}

	public int Next() {
		int value = currentValue;
		if (currentValue == maxValue) {
			currentValue = minValue;
		} else {
			currentValue++;
		}
		return value;
	}

	/**
     * return how much the current value of this sequence is ahead of value
     */
	public int AheadBy(int value) {
		return AheadBy(currentValue, value);
	}

	/**
     * return how much a is ahead of b
     */
	public int AheadBy(int a, int b) {
		if (Mathf.Abs(a - b) < (maxValue - minValue) / 2) {
			return a - b;
		} else if (a > b) {
			return -(maxValue - a + b - minValue + 1);
		} else {
			return maxValue - b + a - minValue + 1;
		}
	}

	/**
     * return how much the current value of this sequence is behind of value
     */
	public int BehindBy(int value) {
		return BehindBy(currentValue, value);
	}

	/**
     * return how much a is behind b
     */
	public int BehindBy(int a, int b) {
		if (Mathf.Abs(a - b) < (maxValue - minValue) / 2) {
			return b - a;
		} else if (a > b) {
			return maxValue - a + b - minValue + 1;
		} else {
			return -(maxValue - b + a - minValue + 1);
		}
	}

	/**
     * return whether the current value of this sequence is newer than value
     */
	public bool Newer(int value) {
		return AheadBy(currentValue, value) > 0;
	}

	/**
     * return whether a is newer than b
     */
	public bool Newer(int a, int b) {
		return AheadBy(a, b) > 0;
	}

	/**
     * return whether the current value of this sequence is newer than or equal to value
     */
	public bool NewerOrEqual(int value) {
		return AheadBy(currentValue, value) >= 0;
	}

	/**
     * return whether a is newer than or equal to b
     */
	public bool NewerOrEqual(int a, int b) {
		return AheadBy(a, b) >= 0;
	}

	/**
     * return whether the current value of this sequence is older than value
     */
	public bool Older(int value) {
		return BehindBy(currentValue, value) > 0;
	}

	/**
     * return whether a is older than b
     */
	public bool Older(int a, int b) {
		return BehindBy(a, b) > 0;
	}

	/**
     * return whether the current value of this sequence is older than or equal to value
     */
	public bool OlderOrEqual(int value) {
		return BehindBy(currentValue, value) >= 0;
	}

	/**
     * return whether a is older than or equal to b
     */
	public bool OlderOrEqual(int a, int b) {
		return BehindBy(a, b) >= 0;
	}
}