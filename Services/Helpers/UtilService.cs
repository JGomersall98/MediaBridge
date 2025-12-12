namespace MediaBridge.Services.Helpers
{
    public interface IUtilService
    {
        string CalculateRunTimeHours(int? runtime);
    }
    public class UtilService : IUtilService
    {
        public string CalculateRunTimeHours(int? runtime)
        {
            if (runtime.HasValue)
            {
                int hours = (int)(runtime / 60);
                int hoursToRemove = hours * 60;
                int minutesLeft = (int)runtime - hoursToRemove;

                string runtimeString = hours + "h " + minutesLeft + "m";
                return runtimeString;
            }
            else
            {
                return "";
            }

        }
    }
}
