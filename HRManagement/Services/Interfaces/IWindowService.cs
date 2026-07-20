namespace HRManagement.Services.Interfaces
{
    public interface IWindowService
    {
        void ShowMainWindow();
        void ShowLoginWindow();

        void Minimize();

        void MaximizeRestore();

        void CloseCurrentWindow();
    }
}