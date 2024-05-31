using Npgsql;
using Core.Const;
using Core.Entity;

namespace Deplom
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            var id = Task.Run(async () => await SecureStorage.GetAsync("id")).Result;
            if (id != null)
            {
                if (Convert.ToInt32(id) > 0)
                {
                    auth.IsVisible = false;
                    display.IsVisible = true;
                    projectListView.ItemsSource = Task.Run(async () => await GetUserProjects(Convert.ToInt32(id))).Result;
                    projectNameEntry.Text = "";
                    projectDescriptionEntry.Text = "";
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
                if (reader.GetString(0) == Password.Text)
                {
                    await DisplayAlert("", "ok", "okey");
                    command = dataSource.CreateCommand($"SELECT id FROM user_entities WHERE name='{Login.Text}'");
                    reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
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

        private async Task<List<Project_entity>> GetUserProjects(int id)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

            await using var dataSource = dataSourceBuilder.Build();

            var command = dataSource.CreateCommand($"SELECT * FROM project_entities WHERE ownerid='{id}'");

            var reader = await command.ExecuteReaderAsync();
            List<Project_entity> listToReturn = new();
            while (reader.Read())
            {
                Project_entity project = new();
                project.Id = (int)reader["Id"];
                project.Name = (string)reader["Name"];
                project.Description = (string)reader["Description"];
                //project.CardIds = reader["cardids"] as List<int>;
                listToReturn.Add(project);
            }
            return listToReturn;
        }

        private void AddProjectButton_Clicked(object sender, EventArgs e)
        {
            AddProjectLayout.IsVisible = true;
            display.IsVisible = false;
        }

        private void AddBackButton_Clicked(object sender, EventArgs e)
        {
            AddProjectLayout.IsVisible = false;
            display.IsVisible = true;
            projectNameEntry.Text = "";
            projectDescriptionEntry.Text = "";
        }

        private async void AddProjectToUserButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);
                await using var dataSource = dataSourceBuilder.Build();
                var command = dataSource.CreateCommand($"INSERT INTO project_entities (name, description, ownerid, cardcounts, cardids, addeduserids)   VALUES ('{projectNameEntry.Text}', '{projectDescriptionEntry.Text}', '{Convert.ToInt32(await SecureStorage.GetAsync("id"))}', '{{0}}', '{{}}', '{{}}')");
                var reader = await command.ExecuteReaderAsync();
                AddProjectLayout.IsVisible = false;
                projectNameEntry.Text = "";
                projectDescriptionEntry.Text = "";
                projectListView.ItemsSource = await GetUserProjects(Convert.ToInt32(await SecureStorage.GetAsync("id")));
                display.IsVisible = true;
                
            }
            catch (Exception ex)
            {
                await DisplayAlert(Title, ex.Message, "ok");
            }
        }

        private async void projectListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var project = e.SelectedItem as Project_entity;
            ProjectNameLabel.Text += project.Name;
            ProjectDescriptionLabel.Text += project.Description;
            projectCards.ItemsSource = await GetProjectCards(project.CardIds);
            ProjectCardsLayout.IsVisible = true;
            display.IsVisible = false;
        }

        private void CardsBackButton_Clicked(object sender, EventArgs e)
        {
            ProjectNameLabel.Text = "Карточки проекта: ";
            ProjectDescriptionLabel.Text = "Описание проекта: ";
            ProjectCardsLayout.IsVisible = false;
            display.IsVisible = true;
        }

        private async Task<List<Card_entity>> GetProjectCards(List<int> cardIds)
        {
            if(cardIds !=  null && cardIds.Count > 0)
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

                await using var dataSource = dataSourceBuilder.Build();

                var command = dataSource.CreateCommand($"SELECT * FROM card_entities");

                var reader = await command.ExecuteReaderAsync();
                List<Card_entity> listToReach = new();
                List<Card_entity> listToReturn = new();
                while (reader.Read())
                {
                    Card_entity card = new();
                    card.Id = (int)reader["Id"];
                    card.Name = (string)reader["Name"];
                    listToReach.Add(card);
                }
                foreach (var card in listToReach)
                {
                    foreach (var cardId in cardIds)
                    {
                        if (card.Id == cardId)
                            listToReturn.Add(card);
                    }
                }
                return listToReturn;
            }
            return new();
        }
    }
}
