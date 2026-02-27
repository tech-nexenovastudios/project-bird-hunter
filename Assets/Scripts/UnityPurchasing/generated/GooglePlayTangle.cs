// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("R0p5gijJF85xsonyeNWki1C0y8+GRNQEe5JvIeXadsl75kI9iPVeV+RW1fbk2dLd/lKcUiPZ1dXV0dTXSJcWenZJm/EjifcikxebUbP9irl/Zdn2z7IiL+gC8j6GdH75qZwcKVbV29TkVtXe1lbV1dRIHYwIy62KzVwDuiFkfLP18ZU2fAI+d1wdm3rGjspqTb1XrICpJx6wLTiAl0Bd8mGmf4YtfSBESniPPsg60S0IrM9cajWZdEeB1rkiF3z8l5tU6s0BCZedWA2p9QTo9m7DD1DTqW/HJQAWl7pwuTgIH5oF/WZK8QfvzrKLsL77V5rwga2wfNvAXMGhVaWkTO8RUDzPcPPZGqBIxICTIbGZKEJfOcdUyZxYkTx7uRNos9bX1dTV");
        private static int[] order = new int[] { 9,13,9,6,11,13,10,13,10,11,12,13,12,13,14 };
        private static int key = 212;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
