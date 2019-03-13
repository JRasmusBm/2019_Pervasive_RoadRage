using ShimmerAPI;
using ShimmerLibrary;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace ShimmerHRGSRStream_ConsoleApp
{
    class Program
    {
        Filter LPF_PPG;
        Filter HPF_PPG;
        PPGToHRAlgorithm PPGtoHeartRateCalculation;
        int NumberOfHeartBeatsToAverage = 1;
        int TrainingPeriodPPG = 10; //10 second buffer
        double LPF_CORNER_FREQ_HZ = 5;
        double HPF_CORNER_FREQ_HZ = 0.5;
        ShimmerLogAndStreamSystemSerialPort Shimmer;
        double SamplingRate = 128;
        int Count = 0;
        bool FirstTime = true;
        string comPort = "COM12";
        string logLocation = @"D:\Tanguy\Documents\LTU\PC\";
        Socket soc;

        //The index of the signals originating from ShimmerBluetooth 
        int IndexGSR;
        int IndexPPG;
        int IndexTimeStamp;

        // K-means params
        int initDelay = 10; // time (in sec) for sensor init
        int kmeansDelay = 30; // time (in sec) for data collecting between each kmeans run
        int numClusters = 3;
        double[][] rawData;
        int[] curClustering;
        double[][] curMeans;
        int prevStressLvl = 0;

        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello");
            Program p = new Program();
            p.start();
        }

        public void initSocket()
        {
            soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = System.Net.IPAddress.Parse("192.168.1.35");
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, 12346);
            soc.Connect(remoteEP);
        }


        public void writeToSocket(int stresslvl)
        {
            byte[] byData;
            switch (stresslvl)
            {
                case 0:
                    byData = System.Text.Encoding.ASCII.GetBytes("high");
                    break;
                case 1:
                    byData = System.Text.Encoding.ASCII.GetBytes("medium");
                    break;
                case 2:
                    byData = System.Text.Encoding.ASCII.GetBytes("low");
                    break;
                default:
                    byData = System.Text.Encoding.ASCII.GetBytes("medium");
                    break;
            }
             
            soc.Send(byData);
        }


        public void start()
        {
            System.IO.File.Delete(logLocation + "dataGSR.txt");
            System.IO.File.Delete(logLocation + "stressLevelsLog.txt");
            System.IO.File.Delete(logLocation + "stressLevelsStream.txt");

            initSocket();

            //Setup PPG to HR filters and algorithm
            PPGtoHeartRateCalculation = new PPGToHRAlgorithm(SamplingRate, NumberOfHeartBeatsToAverage, TrainingPeriodPPG);
            LPF_PPG = new Filter(Filter.LOW_PASS, SamplingRate, new double[] { LPF_CORNER_FREQ_HZ });
            HPF_PPG = new Filter(Filter.HIGH_PASS, SamplingRate, new double[] { HPF_CORNER_FREQ_HZ });

            //Init raw data collector
            rawData = new double[kmeansDelay][];

            int enabledSensors = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_GSR | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_INT_A13);


            Shimmer = new ShimmerLogAndStreamSystemSerialPort("ShimmerID1", comPort, SamplingRate, 0, ShimmerBluetooth.GSR_RANGE_AUTO, enabledSensors, false, false, false, 1, 0, Shimmer3Configuration.EXG_EMG_CONFIGURATION_CHIP1, Shimmer3Configuration.EXG_EMG_CONFIGURATION_CHIP2, true);

            Shimmer.UICallback += this.HandleEvent;
            Shimmer.Connect();

        }
        public void HandleEvent(object sender, EventArgs args)
        {
            CustomEventArgs eventArgs = (CustomEventArgs)args;
            int indicator = eventArgs.getIndicator();

            switch (indicator)
            {
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_STATE_CHANGE:
                    System.Diagnostics.Debug.Write(((ShimmerBluetooth)sender).GetDeviceName() + " State = " + ((ShimmerBluetooth)sender).GetStateString() + System.Environment.NewLine);
                    int state = (int)eventArgs.getObject();
                    if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTED)
                    {   
                        System.Console.WriteLine("Shimmer is Connected");
                        Task ignoredAwaitableResult = this.delayedWork();
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTING)
                    {
                        System.Console.WriteLine("Establishing Connection to Shimmer Device");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_NONE)
                    {
                        System.Console.WriteLine("Shimmer is Disconnected");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_STREAMING)
                    {
                        System.Console.WriteLine("Shimmer is Streaming");
                    }
                    break;
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_NOTIFICATION_MESSAGE:
                    break;
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_DATA_PACKET:
                    ObjectCluster objectCluster = (ObjectCluster)eventArgs.getObject();
                    if (FirstTime)
                    {
                        IndexGSR = objectCluster.GetIndex(Shimmer3Configuration.SignalNames.GSR, ShimmerConfiguration.SignalFormats.CAL);
                        IndexPPG = objectCluster.GetIndex(Shimmer3Configuration.SignalNames.INTERNAL_ADC_A13, ShimmerConfiguration.SignalFormats.CAL);
                        IndexTimeStamp = objectCluster.GetIndex(ShimmerConfiguration.SignalNames.SYSTEM_TIMESTAMP, ShimmerConfiguration.SignalFormats.CAL);
                        FirstTime = false;
                    }
                    SensorData dataGSR = objectCluster.GetData(IndexGSR);
                    SensorData dataTS = objectCluster.GetData(IndexTimeStamp);


                    if (Count % SamplingRate == 0) //only display data every second
                    {
                        // LOG HR and GSR values
                        System.IO.File.AppendAllText(logLocation+"dataGSR.txt", dataGSR.Data+Environment.NewLine);
                        System.Console.WriteLine("Time Stamp: "+ dataTS.Data+ " " + dataTS.Unit + " GSR: " + dataGSR.Data + " "+ dataGSR.Unit);
                        if(Count < SamplingRate * initDelay)
                        { // Before the end of 10s init delay
                            System.Console.WriteLine(" - Data skipped (init delay)");
                        }
                        else
                        {
                            double[] newDataPoint = new double[] { dataGSR.Data };
                            rawData[Convert.ToInt32((Count / SamplingRate) - initDelay)%kmeansDelay] = newDataPoint;
                            if (Count > (kmeansDelay+initDelay+1) * SamplingRate && curMeans != null)
                            { // At least the first k means has to have been performed

                                int stressLevel = findBestLabel(newDataPoint);
                                System.IO.File.AppendAllText(logLocation + "stressLevelsLog.txt", stressLevel + Environment.NewLine);
                                if (stressLevel != prevStressLvl)
                                {
                                    prevStressLvl = stressLevel;
                                    writeToSocket(stressLevel);
                                    System.IO.File.WriteAllText(logLocation + "stressLevelStream.txt", stressLevel + Environment.NewLine);
                                }
                            }
                        }

                        if (Count >= (kmeansDelay+initDelay+0.6)*SamplingRate
                            && Convert.ToInt32((Count / SamplingRate)-initDelay) % kmeansDelay == 0)
                        {   // Run kmeans every 'kmeansDelay' seconds to update means
                            System.Console.WriteLine("Running new K-means");
                            (curClustering, curMeans) = Cluster(rawData, numClusters);
                            Array.Sort(curMeans, (v1, v2) => v1[0] > v2[0] ? 1 : v2[0] < v1[0] ? -1 : 0);

                            //Display clusters means
                            for (int i = 0; i < curMeans.Length; i++)
                            {
                                System.Console.Write("Cluster " + i + "\t");
                                for (int j = 0; j < curMeans[i].Length; j++)
                                {
                                    System.Console.Write(curMeans[i][j] + "\t");
                                }
                                System.Console.Write("\n");
                            }
                        }
                    }
                    


                    Count++;
                    break;
            }
        }

        private async Task delayedWork()
        {
            await Task.Delay(1000);
            Shimmer.StartStreaming();
        }

        public static (int[] clustering, double[][] means) Cluster(double[][] rawData, int numClusters)
        {
            // k-means clustering
            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            //double[][] data = Normalized(rawData); // so large values don't dominate

            double[][] data = rawData;

            bool changed = true; // was there a change in at least one cluster assignment?
            bool success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            int[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
            double[][] means = Allocate(numClusters, data[0].Length); // small convenience

            int maxCount = data.Length * 10; // sanity check
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct; // k-means typically converges very quickly
                success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
            }
            // consider adding means[][] as an out parameter - the final means could be computed
            // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
            // and even though you can compute final means from final clustering, in some cases it
            // makes sense to return the means (at the expense of some method signature uglinesss)
            //
            // another alternative is to return, as an out parameter, some measure of cluster goodness
            // such as the average distance between cluster means, or the average distance between tuples in 
            // a cluster, or a weighted combination of both
            return (clustering, means);
        }

        private static double[][] Normalized(double[][] rawData)
        {
            // normalize raw data by computing (x - mean) / stddev
            // primary alternative is min-max:
            // v' = (v - min) / (max - min)

            // make a copy of input data
            double[][] result = new double[rawData.Length][];
            for (int i = 0; i < rawData.Length; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j) // each col
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = colSum / result.Length;
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters); // other assignments random
            return clustering;
        }

        private static double[][] Allocate(int numClusters, int numColumns)
        {
            // convenience matrix allocator for Cluster()
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to means[][]

            // update, zero-out means so it can be used as scratch matrix 
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j]; // accumulate sum
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k]; // danger of div by 0
            return true;
        }

        private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length]; // proposed result
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters]; // distances from curr tuple to each mean

            for (int i = 0; i < data.Length; ++i) // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]); // compute distances from curr tuple to all k means

                int newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID; // update
                }
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static double Distance(double[] tuple, double[] mean)
        {
            // Euclidean distance between two vectors for UpdateClustering()
            // consider alternatives such as Manhattan distance
            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        // Finds best corresponding level of stress
        // computing minimum distance to each cluster center (=mean)
        private int findBestLabel(double[] newData)
        {
            double[] distances = new double[numClusters]; // Euclidean distances collector
            int minIndex = 0;
            for (int i=0; i<numClusters; i++)
            {
                //distances[i] = Math.Sqrt(Math.Pow((newData[0]-curMeans[i][0]), 2) + Math.Pow((newData[1] - curMeans[i][1]), 2));
                distances[i] = Math.Abs(newData[0]-curMeans[i][0]);
                if (distances[i] < distances[minIndex]) { minIndex = i; }
            }
            return minIndex;
        }
    }
}
