using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; }

    public App(DatabaseService databaseService)
    {
        InitializeComponent();

        Database = databaseService;

        //Database.Init();

        MainPage = new AppShell();
    }
}