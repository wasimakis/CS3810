﻿//Code by William Asimakis 
//CS 3810 Study Assignment 8
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cache_Simulator
{
    class Program
    {
        //The base cycle penalty for a cache miss. 
        private const int CPENALTY = 20;
        //The max amount of Bits we can have in a cache system. 
        private const int MAXBITS = 860;
        //An array of 16-bit addresses that will simulated with cache misses and hits 
        private static ushort[] addresses = { 4, 8, 20, 24, 28, 36, 44, 20, 28, 36, 40, 44, 68, 72, 92, 96, 100, 104, 108, 112, 100, 112, 116, 120, 128, 140 };
     
        static void Main(string[] args)
        {
            //Assuming no CPI could ever get this high. 
            double lowestCPI = 700;
            int lowB = 0;
            int lowR = 0;
            int lowW = 0;
            char lowA = 'g';
            int dataBytes = 4;
            int numRows = 1;
            int numWays = 1; 
            //======================ATTEMPT TO FIND FASTEST CACHE ARCHITECTURE===================================
            for (int changeB = dataBytes; changeB <= 64; changeB = (int)Math.Pow(changeB, 2)) {
                //Assuming the rows could never go past 40. 
                for (int changeR = numRows; changeR <= 40; changeR++) {
                    //Assuming the number of ways could not go past 4. 
                    for (int changeW = numWays; changeW <= 4; changeW++) {
                        for (int architecture = 0; architecture <= 2; architecture++) {
                            char choose;
                            switch (architecture) {
                                case 0:
                                    choose = 'f';
                                    break;
                                case 1:
                                    choose = 'd';
                                    break;
                                case 2:
                                    choose = 's';
                                    break;
                                default:
                                    choose = 'f';
                                    break;
                            }
                            if (Simulate(changeB, changeR, choose, changeW, out double avgCpi)) {
                                if (avgCpi < lowestCPI) {
                                    lowestCPI = avgCpi;
                                    lowB = changeB;
                                    lowR = changeR;
                                    lowA = choose;
                                    lowW = changeW;
                                }
                            }
                        }
                    }
                }
            }
            String arch = "";
            switch (lowA) {
                case 'f':
                    arch = "Fully Associative";
                    break;
                case 'd':
                    arch = "Direct Mapping";
                    break;
                case 's':
                    arch = "Set Associative";
                    break;
            }
            Console.WriteLine("The fastest cache architecture is a " + arch + " whose avg CPI is " + lowestCPI + ".  This architecture has " +  
                lowW + " number of ways, " + lowB + " number of bytes and " + lowR + " amount of rows in the architecture");
            //The amount of Bytes per data chunk per row. 
             dataBytes = 16;
            //The amount of Rows for the cache system. 
             numRows = 2;
             numWays = 3;
           // Simulate(dataBytes, numRows,s', numWays, out double nothing);
            //Console.WriteLine("Finished Fully Associative Simulation");
            //Simulate(dataBytes, numRows, 's', numWays, out double nothing);
            Console.Read();


        }

        //Simulates cache architecture for a specific amount of dataBytes and the number of rows. Different simulation options will mimic direct mapping or full or set
        //associativity. The num ways parameter is used exclusively to simulate  set associativity.
        private static bool Simulate(int dataBytes, int numRows, char chooser, int numWays, out double avgCpi)
        {
            if (chooser == 'f')
            {
                Console.WriteLine("Now Simulating Fully Associative!");
            }
            //Compute the total amount of bits per data chunk. 
            int dataBits = dataBytes * 8;
            //Compute the total amount of bits per tag.  
            int tagBits = 0;
            //If we are using direct mapping, the tag bit amount is reduced due to additional space needed for mapping. 
            int directMap = 0;
            //Used to compute the reduced directmap due to a computational error. 
            if (chooser != 'f')
            {
                double _dirMap = (Math.Log(numRows) / Math.Log(2));
                if (_dirMap <= 0 || _dirMap % 1 != 0)
                {
                    Console.WriteLine("Cannot compute for number of rows that are not a base of 2 or negative");
                    avgCpi = Int16.MaxValue;
                    return false;
                }
                directMap = (int)_dirMap;
            }
            double _tagBits = 16 - directMap - (Math.Log(dataBytes) / Math.Log(2));
            if (_tagBits <= 0 || _tagBits % 1 != 0)
            {
                Console.WriteLine("Cannot compute for amount of data bytes that are not a base of 2 or negative");
                avgCpi = Int16.MaxValue;
                return false;
            }
            tagBits = (int)_tagBits;


            //Compute the total amount of bits for the Least Recently Used.  
            int lruBits = 0;
            if (chooser != 'd')
            {
                int lruChooser = 1;
                //Notice that the LRU in a set associative is only used across the rows, and essentially acts as a fully associative across the amount of set ways.  
                if (chooser == 's')
                {
                    lruChooser = numWays;
                }
                else {
                    lruChooser = numRows;
                }

                double _lruBits = Math.Log(lruChooser) / Math.Log(2);
                //If there is any remainder, that will have to translate into an additional bit space.
                if (_lruBits % 1 > 0 && _lruBits >= 1)
                {
                    _lruBits++;
                    lruBits = (int)_lruBits;
                }
                else
                {
                    lruBits = (int)_lruBits;
                }
            }

                     
            //Compute the total amount of bits that this cache structure will utilize:  
            int totalBits = (lruBits + tagBits + dataBits + 1) * numRows * numWays;

            if (totalBits > MAXBITS)
            {
                Console.WriteLine("Cannot construct a cache of size " + totalBits + " because it is more than the maximum size of " + MAXBITS);
                avgCpi = Int16.MaxValue;
                return false;
            }
            Console.WriteLine("Current cache architecture has a total bit size of " + totalBits + ", which is within maximum size limit");


            //Based upon the available data we can construct the architecture: (IMPORTANT DISCLAIMER -- The following structuer is meant to simulate 
            // a cache system and it might not be accurate of how the real physical electronic system functions! Therefore, this should not be used for optimization analysis) 
            //  x   --- a specific section of the cache (used in set associtivity, 0 otherwise)
            //  [x,0] --- The tag 
            //  [x,1] --- The Validitiy(0 or 1)
            //  [x,2] ---  The LRU (should be anywhere from 0-9 (decimal wise) (WILL NOT BE UTILIZIED IN DIRECT MAPPING)
            //  NOTE: There is a data section but it is not important in this simulation  
            //Also note that the first
            //Here is the construction of the architecure, we want everything to be zero so this is an adequate initial cache construction): 
            int[,,] cache = new int[numWays, numRows, 3];


            //Store the amount of hits and misses; 
            int hits = 0;
            int misses = 0;
            int cycleAmount = 2;
            //Cycle the pattern a few times 
            for (int cycle = 1; cycle <= cycleAmount; cycle++)
            {

                //Start executing the address bits. 
                for (int addr = 0; addr < addresses.Length; addr++)
                {
                    int tag = 0;
                    if (chooser != 'f')
                    {
                        tag = addresses[addr] / (dataBytes * numRows);
                    }
                    else {
                        tag = addresses[addr] / dataBytes;
                    }
                    
                    //Strictly used in set associativity and direct mapping.
                    int row = (addresses[addr] / dataBytes) % numRows;
                    if (cycle != 1)
                    {
                        Console.Write("Accessing " + addresses[addr] + "(tag = " + addresses[addr] / dataBytes + "):");
                    }
                    //Search for the corresponding tag:  
                    bool hit = false;
                    int hitRow = 0;
                    //Used for set associative
                    int hitWay = 0;
                    //For full associativity
                    if (chooser == 'f')
                    {
                        for (int r = 0; r < numRows; r++)
                        {
                            //We found a row with the correct tag.  
                            if (cache[0, r, 0] == tag)
                            {
                                //The cache is validated
                                if (cache[0, r, 1] == 1)
                                {
                                    //Determine which row we hit on. 
                                    hitRow = r;
                                    //break and set the hit to true;
                                    hit = true;
                                    //Record the hit cycle penalties for a hit post-first go around. 
                                    if (cycle != 1)
                                    {
                                        hits += 1;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    //For direct mapping 
                    else if (chooser == 'd')
                    {
                        //We know the row from the equation to find the tag. 
                        if (cache[0, row, 0] == tag)
                        {
                            if (cache[0, row, 1] == 1)
                            {
                                //Determine which row we hit on. 
                                hitRow = row;
                                //break and set the hit to true;
                                hit = true;
                                //Record the hit cycle penalties for a hit post first go around. 
                                if (cycle != 1)
                                {
                                    hits += 1;
                                }
                            }
                        }
                    } 
                    //For set associativity
                    else if (chooser == 's')
                    {
                        //Attempt to find the tag through full associtavity but with the corresponding direct mapped row. 
                        for (int r = 0; r < numWays; r++) {
                            if (cache[r, row, 0] == tag) {
                                if (cache[r, row, 1] == 1) {
                                    hitRow = row;
                                    hitWay = r;
                                    hit = true;
                                    //Record the hit cycle penalties for a hit post first go around. 
                                    if (cycle != 1)
                                    {
                                        hits += 1;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        avgCpi = Int16.MaxValue;
                        return false;
                    }
                    //---------------------FINISHED ATTEMPT AT FINDING TAG-----------------
                    if (hit && cycle != 1)
                    {
                        if (chooser != 's')
                        {
                            Console.Write("hit from row " + hitRow + "\n");
                        }
                        else {
                            Console.Write("hit from row " + hitRow + " on cache way " + hitWay + "\n");
                        }
                    }
                    //We didn't hit, which means we have to "cache". 
                    else
                    {
                        //If it is direct mapping we simply have to update the tag and suffer the penalties. 
                        if (chooser == 'd')
                        {
                            //Increment the misses track post-first cycle.  
                            if (cycle != 1)
                            {
                                
                                Console.Write("miss - cached to row " + row + "\n");
                                misses += CPENALTY + dataBytes;
                            }
                            //"cache" the new data (which happens within the processor architecture but for sake of the simulation will do nothing) 
                            //Revalidate the direct mapped row that has "cached" the new data, along with the new tag information (also validate if it has not been). 
                            cache[0, row, 0] = tag;
                            cache[0, row, 1] = 1;
                        }
                        else
                        {
                            int lruAmount = 0;
                            int lruRow = 0; 
                            //Search the span of the single cache for the lru
                            if (chooser == 'f')
                            {
                                //Find the lru (which is the counted most variable)
                                for (int r = 0; r < numRows; r++)
                                {
                                    if (cache[0, r, 2] > lruAmount)
                                    {
                                        lruAmount = cache[0, r, 2];
                                        lruRow = r;
                                    }
                                }
                            } 
                            //search the span of multiple way caches
                            else {
                                for (int r = 0; r < numWays; r++) {
                                    if (cache[r, row, 2] > lruAmount) {
                                        lruAmount = cache[r, row, 2];
                                        lruRow = r;
                                    }
                                }
                            }

                            //"cache" the new data (which happens within the processor architecture but for sake of the simulation will do nothing)  

                            //Increment the misses track post-first cycle.  
                            if (cycle != 1)
                            {
                               
                                if (chooser == 'f')
                                {
                                    Console.Write("miss - cached to row" + row + " in LRU cache way " + lruRow + "\n");
                                }
                                else {
                                    Console.Write("miss - cached to LRU row " + lruRow + "\n");
                                }
                               
                                misses += CPENALTY + dataBytes;
                            }
                            if (chooser == 'f')
                            {
                                //Revalidate the lru row that has "cached" the new data, along with the new tag information. 
                                cache[0, lruRow, 0] = tag;
                                cache[0, lruRow, 1] = 1;
                                //Go back and increment all lru except the lruRow for this instance. 
                                for (int r = 0; r < numRows; r++)
                                {
                                    if (r != lruRow)
                                    {
                                        cache[0, r, 2]++;
                                    }
                                }
                            }
                            else {
                                //Revalidate the lru row that has "cached" the new data, along with the new tag information. 
                                cache[lruRow, row, 0] = tag;
                                cache[lruRow, row, 1] = 1; 

                                //Evaluate through Each way in the set associative to determine new LRU amounts. 
                                for (int r = 0; r < numWays; r++) {
                                    if (r != lruRow) {
                                        cache[r, row, 2]++;
                                    }
                                }
                            }

                        }
                    }
                }
                if (cycle == 1)
                {
                    Console.WriteLine("Finished preliminary setup...Advancing to significant pattern cycle");
                }
            }
            //Calculate the cycles per instruction. 
            double cpi = (hits + misses) / (double)((cycleAmount - 1) * addresses.Length);
            Console.WriteLine("Current average CPI of cache architecture: " + cpi);
            avgCpi = cpi;
            return true;
        }
 
    }
}
