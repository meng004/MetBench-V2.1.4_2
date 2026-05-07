public class sin {
    public static double sin(double x) {
        double sin = 0;
        double term = x;
        int i = 1;
        while (Math.abs(term) > 1e-10) {
            sin += term;
            term *= -(x * x) / ((2 * i) * (2 * i + 1));
            i++;
        }
        return sin;
    }
}