using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Struct for WinLossRecord to use to organize data of specific win/loss ratios between players
    /// </summary>
    [Serializable]
    public class WinLossObject
    {
        // Name of the WinLossObject. Should match its key in WinLossRecord's WinLossRecord dictionary - The game's name
        public string name { get; private set; }
        // Number of wins in this game against a certain player
        public long numWins { get; private set; }
        // Number of losses in this game against a certain player
        public long numLosses { get; private set; }
        // Percentage of wins over course of all matches
        public float winPercentage { get; private set; }
        // Net MMR gain/loss over the course of all matches
        public double totalMMRChange { get; private set; }

        /// <summary>
        /// Basic constructor for WinLossObject
        /// </summary>
        public WinLossObject(string nameIn)
        {
            this.name = nameIn;
            this.numWins = 0;
            this.numLosses = 0;
            this.winPercentage = 0;
            this.totalMMRChange = 0;
        }

        /// <summary>
        /// Adds a win into the object
        /// </summary>
        public void AddWin(double mmrShift)
        {
            // Increment win counter
            numWins++;
            // Recalculate win/loss percentage
            winPercentage = CalculateWinLossPercentage();
            // Adjust total MMR change
            totalMMRChange += mmrShift;
        }

        /// <summary>
        /// Adds a loss into the object
        /// </summary>
        /// <param name="mmrShift"></param>
        public void AddLoss(double mmrShift)
        {
            // Increment loss counter
            numLosses++;
            // Recalculate win/loss percentage
            winPercentage = CalculateWinLossPercentage();
            // Adjust total MMR change
            totalMMRChange += mmrShift;
        }

        /// <summary>
        /// Calculates win percentage of this WinLossObject
        /// </summary>
        /// <returns>Win percentage as a float</returns>
        public float CalculateWinLossPercentage()
        {
            return (numWins / Math.Max(numLosses, 1)); // Let's not divide by zero...
        }
    }
}
