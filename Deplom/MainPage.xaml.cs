using Microsoft.Extensions.Logging;
using Npgsql;
using Core.Const;

namespace Deplom
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            var id = Task.Run(async () =>  await SecureStorage.GetAsync("id") ).Result;
            if(id != null)
            {
                if(Convert.ToInt32(id) > 0)
                {
                    auth.IsVisible = false;
                    display.IsVisible = true;
                }
            }
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);
           
            await using var dataSource = dataSourceBuilder.Build();

            var command = dataSource.CreateCommand($"SELECT password FROM user_entities WHERE name='{Login.Text}'");
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
               if(reader.GetString(0) == Password.Text)
                {
                    await DisplayAlert("", "ok", "okey");
                    command = dataSource.CreateCommand($"SELECT id FROM user_entities WHERE name='{Login.Text}'");
                    reader = await command.ExecuteReaderAsync();
                    while(await reader.ReadAsync())
                    {
                        await SecureStorage.SetAsync("id", reader.GetInt32(0).ToString());
                    }
                    auth.IsVisible = false;
                    display.IsVisible = true;
                }
                else { await DisplayAlert("", "bet", "okey"); }
            }
        }

        private void logaut_Clicked(object sender, EventArgs e)
        {
            SecureStorage.RemoveAll();
            auth.IsVisible = true;
            display.IsVisible = false;

        }

        private async void Reg_Clicked(object sender, EventArgs e)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

            await using var dataSource = dataSourceBuilder.Build();

            var command = dataSource.CreateCommand($"INSERT INTO user_entities (name, password, projectids)   VALUES ('{Login.Text}', '{Password.Text}', ' {{0}}')");

            var reader = await command.ExecuteReaderAsync();
        }
    }

}
