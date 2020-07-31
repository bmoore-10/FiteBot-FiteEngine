using FiteEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine
{
    public class GlickoEngine
    {
        private float conversionConstant = 173.7178f;
        /// <summary>
        /// Constructor - Creates a new instance of the GlickoEngine
        /// </summary>
        public GlickoEngine()
        {
        }

        /// <summary>
        /// Handles calculating the outcome of a match as reported by its players.
        /// </summary>
        /// <param name="initiatingPlayer"></param>
        /// <param name="challengedPlayer"></param>
        /// <returns>An array featuring two MatchResult structs. The first is the initiating player's results, the second is the challenged player's results</returns>
        public MatchResult[] HandleReport(Match inMatch)
        {
            // Get each player's rating in the current game first
            GameRanking initatingPlayerRanking = inMatch.initiatingPlayer.getGameRanking(inMatch.game.fullTitle);
            GameRanking challengedPlayerRanking = inMatch.challengedPlayer.getGameRanking(inMatch.game.fullTitle);
            double gameTau = inMatch.game.tau;

            MatchResult initiatingPlayerResults;
            MatchResult challengedPlayerResults;
            // Get the results for each player and return them
            if(inMatch.trueVictor.GetName().Equals(inMatch.initiatingPlayer.GetName()))
            {
                // If initiating player won, run the report helper like so
                initiatingPlayerResults = HandleReportHelper(initatingPlayerRanking, challengedPlayerRanking, gameTau, true, inMatch.game, inMatch.challengedPlayer);
                challengedPlayerResults = HandleReportHelper(challengedPlayerRanking, initatingPlayerRanking, gameTau, false, inMatch.game, inMatch.initiatingPlayer);
            }
            else
            {
                // Otherwise, tell the report helper the challenger won
                initiatingPlayerResults = HandleReportHelper(initatingPlayerRanking, challengedPlayerRanking, gameTau, false, inMatch.game, inMatch.challengedPlayer);
                challengedPlayerResults = HandleReportHelper(challengedPlayerRanking, initatingPlayerRanking, gameTau, true, inMatch.game, inMatch.initiatingPlayer);
            }

            return new MatchResult[2] { initiatingPlayerResults, challengedPlayerResults };
        }

        /// <summary>
        /// Helper for handling match victory reports. Goes through and generates a MatchResult for a player
        /// based upon whether they won or loss the game.
        /// </summary>
        /// <param name="mainRanking">The player for whoms't the result will be generated for</param>
        /// <param name="opponentRanking"> The opponent</param>
        /// <param name="victory">Whether or not the mainRanking player won the match or not</param>
        /// <returns></returns>
        private MatchResult HandleReportHelper(GameRanking mainRanking, GameRanking opponentRanking, double gameTau, bool victory, Game game, Player opponent)
        {
            // Step 2, convert both players' ratings and deviations onto the glicko-2 scale
            double mainPlayerMu = calculateMu(mainRanking);
            double opponentMu = calculateMu(opponentRanking);

            double mainPlayerPhi = calculatePhi(mainRanking);
            double opponentPhi = calculatePhi(opponentRanking);

            // Step 3, compute the quantity v
            double opponentGPhi = calculateGPhi(opponentPhi);
            double opponentGPhiSquared = Math.Pow(opponentGPhi, 2);
            double bigE = calculateBigE(opponentGPhi, mainRanking.rating, opponentRanking.rating);
            double vBeforeInversion = opponentGPhiSquared * bigE * (1 - bigE);
            double v = Math.Pow(vBeforeInversion, -1);

            // Step 4, Compute the quantity delta
            int score = 0;
            if(victory) { score = 1; }
            double delta = v * ( opponentGPhi * (score - bigE));

            // Step 5, compute σ′- the new volatility value
            // 5.1
            double convergenceTolerance = 0.000001f;
            double alpha = Math.Log(Math.Pow(mainRanking.volatility, 2));
            // 5.2 - Set the initial values of hte iterative algorithm
            double bigA = alpha;
            double bigB;
            if(Math.Pow(delta, 2) > (Math.Pow(mainPlayerPhi, 2) + v))
            {
                bigB = Math.Log(Math.Pow(delta, 2) - Math.Pow(mainPlayerPhi, 2) - v);
            }
            else
            {
                int k = 1;
                double aMinusKTau = alpha - (k * gameTau);
                double fOfX = computeFofX(delta, mainPlayerPhi, v, gameTau, alpha, aMinusKTau);
                while(fOfX < 0)
                {
                    k++;
                    aMinusKTau = alpha - (k * gameTau);
                    fOfX = computeFofX(delta, mainPlayerPhi, v, gameTau, alpha, aMinusKTau);
                }
                bigB = aMinusKTau;
            }
            // 5.3 - computer f(A) and f(B)
            double fA = computeFofX(delta, mainPlayerPhi, v, gameTau, alpha, bigA);
            double fB = computeFofX(delta, mainPlayerPhi, v, gameTau, alpha, bigB);
            // 5.4 - While |B-A| > convergenceTolerance, run the iterative alg
            while(Math.Abs(bigB - bigA) > convergenceTolerance)
            {
                //5.4.a
                double fBminusfA = fB - fA;
                double bigAMinusbigBtimesfA = (bigA - bigB) * fA;
                double bigC = bigA + (bigAMinusbigBtimesfA / fBminusfA);
                double fC = computeFofX(delta, mainPlayerPhi, v, gameTau, alpha, bigC);
                //5.4.b
                if( (fC * fB) < 0)
                {
                    bigA = bigB;
                    fA = fB;
                }
                else
                {
                    fA = fA / 2;
                }
                //5.4.c
                bigB = bigC;
                fB = fC;
                //5.4.d - Loop around and stop when |B-A| <= convergenceTolerance
            }
            // 5.5 - Determine new volatility
            double newVolatility = Math.Pow(Math.E, (bigA / 2));

            // Step 6 - Determine new pre-rating period deviation φ*
            double phiStar = Math.Sqrt(Math.Pow(mainPlayerPhi, 2) + Math.Pow(newVolatility, 2));

            // Step 7 - Determine new unconverted rating (φ') and new unconverted deviation(μ')
            double newPhiLHS = 1 / Math.Pow(phiStar, 2);
            double newPhiRHS = 1 / v;
            double newPhiDenom = Math.Sqrt(newPhiLHS + newPhiRHS);
            double newPhi = 1 / newPhiDenom;

            double newMuInside = opponentGPhi * (score - bigE);
            double newMuRight = Math.Pow(newPhi, 2) * newMuInside;
            double newMu = mainPlayerMu + newMuRight;

            // Step 8 - Convert to determine new rating and deviations
            double newRating = (conversionConstant * newMu) + 1500;
            double newDeviation = conversionConstant * newPhi;

            // Return a matchresult relating to the relative stat changes of MainPlayer
            return new MatchResult(game, opponent, victory, mainRanking.rating, newRating, newDeviation, newVolatility);
        }

        /// <summary>
        /// Used for calculating the μ value of a player in Glicko-2 implementation (Step 2)
        /// </summary>
        /// <param name="inPlayer"></param>
        /// <returns>Double representing converted μ value of player's ranking</returns>
        private double calculateMu(GameRanking inRanking)
        {
            double intermediateStep = inRanking.rating - 1500;
            return intermediateStep / conversionConstant;
        }

        /// <summary>
        /// Used for calculating the φ value of a player in Glicko-2 implementation (Step 2)
        /// </summary>
        /// <param name="inRanking"></param>
        /// <returns>Double representing converted φ value of player's deviation</returns>
        private double calculatePhi(GameRanking inRanking)
        {
            return inRanking.deviation / conversionConstant;
        }

        /// <summary>
        /// Used for calculating the g(φ) value in step 3 of Glicko-2 implmentation
        /// </summary>
        /// <param name="phi"></param>
        /// <returns>A double representing g(φ)</returns>
        private double calculateGPhi(double phi)
        {
            double divisorIntermediate = 3 * (Math.Pow(phi, 2) / Math.Pow(Math.PI, 2));
            double divisor = Math.Sqrt(1 + divisorIntermediate);
            return 1 / divisor;
        }

        /// <summary>
        /// Used for calculating the E(μ,μj,φj) value in step 3 of Glicko-2 implmentation
        /// </summary>
        /// <returns>A double representing E(μ,μj,φj)</returns>
        private double calculateBigE(double opponentGPhi, double mainRating, double opponentRating)
        {
            double negativeGPhi = opponentGPhi * -1;
            double ratingsSubtracted = mainRating - opponentRating;
            double divisor = 1 + Math.Pow(negativeGPhi, ratingsSubtracted);
            return 1 / divisor;
        }

        /// <summary>
        /// The function used to computer f(x) for step 5 of Glicko-2
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="mainPlayerPhi"></param>
        /// <param name="v"></param>
        /// <param name="tau"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private double computeFofX(double delta, double mainPlayerPhi, double v, double tau, double alpha, double x)
        {
            double rhs = (x - alpha) / (Math.Pow(tau, 2));
            double lhsNumerator = Math.Pow(Math.E, x) * ((Math.Pow(delta, 2) - Math.Pow(mainPlayerPhi, 2) - v - Math.Pow(Math.E, x)));
            double lhsDenom = 2 * (Math.Pow((Math.Pow(mainPlayerPhi, 2) + v + Math.Pow(Math.E, x)), 2));
            return (lhsNumerator / lhsDenom) - rhs;
        }
    }
}
