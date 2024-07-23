namespace ImageAnalyzer.ViewModel
{
    public class AnalysisResultViewModel
    {
        public string AnnotatedImagePath { get; set; }
        public string Caption { get; set; }
        public List<string> DenseCaptions { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Objects { get; set; }
        public List<string> People { get; set; }
    }

}
