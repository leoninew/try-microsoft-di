<Query Kind="Program" />

void Main() {
    var path = @"<you path>\bin\Debug\netcoreapp3.0\logs\<your info log name>.log";
    var lines = File.ReadLines(path)
        .Select(Parse)
        .Dump(toDataGrid:true);
}


String pattern = @"method (?<method>\S+), cache (?<cache>\S+), target (?<target>.+), cost (?<cost>\d+)";

Object Parse(String line) {
    var match = Regex.Match(line, pattern);
    var count = -1;
    if (match.Groups["count"].Success) {
        count = Int32.Parse(match.Groups["count"].Value);
    }
    var target = match.Groups["target"].Value;
    if (count != -1) {
        target = String.Format("from IFoo_{0} to IFoo_{1}", 0, count);
    }
    return new Record {
        Method = match.Groups["method"].Value,
        Target = target,
        Cache = Boolean.Parse(match.Groups["cache"].Value),
        Cost = Double.Parse(match.Groups["cost"].Value),
    };
}


class Record {
    public String Method;
    public String Target;
    public Boolean Cache;
    public Double Cost;
}