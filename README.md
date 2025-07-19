with a simple DataCompressorManager.Compress(double[] values) you get compressed values with a delta count variable compression
Leading to 2:1 for complexe sensors to 8:1 for slowest ones. 
These compression stack well with ZIP compression leading to an average compression of 36:1
exemple 100 M doubles (800Mo) finish into a 20Mo Archive.

Also handles NaN Values
