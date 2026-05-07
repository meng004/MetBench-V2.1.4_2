public class cos {
public static double cos(double x) {
        int terms = 10;
        double result = 0;
        double sign = 1;
        double power = 1;
        int factorial = 1;
        for (int i = 0; i < terms; i++) {
            result += sign * power / factorial;
            sign *= -1;
            power *= x * x;
            factorial *= (2 * i + 1) * (2 * i + 2);
        }
        return result;
    }
}