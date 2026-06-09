namespace MetBench_Client.Services;

public interface INavigationAware
{
    void OnNavigatedTo();

    void OnNavigatedFrom();
}
