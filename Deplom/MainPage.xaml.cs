using Npgsql;
using Core.Const;
using Core.Entity;

namespace Deplom
{
    public partial class MainPage : ContentPage
    {
        Project_entity current_project;

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
                await DisplayAlert("Процесс", "Идёт авторизация", "okey");
                if ((string)reader["password"] == Password.Text)
                {
                    await DisplayAlert("", "ok", "okey");
                    command = dataSource.CreateCommand($"SELECT id FROM user_entities WHERE name='{Login.Text}'");
                    reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        await SecureStorage.SetAsync("id", Convert.ToString((int)reader["id"]));
                    }
                    auth.IsVisible = false;
                    display.IsVisible = true;
                }
                else { await DisplayAlert("Ошибка", "При авторизации произошла ошибка", "okey"); }
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

            var command = dataSource.CreateCommand($"INSERT INTO user_entities (name, password) VALUES ('{Login.Text}', '{Password.Text}');");
            await DisplayAlert("Процесс", "Идёт регистрация", "ОК");
            await command.ExecuteNonQueryAsync();
            await DisplayAlert("Успех", "Вы успешно зарегистрированы, авторизуйтесь", "ОК");
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
                project.CardIds = (string)reader["CardIds"];
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
                var command = dataSource.CreateCommand($"INSERT INTO project_entities (name, description, ownerid, cardids)   VALUES ('{projectNameEntry.Text}', '{projectDescriptionEntry.Text}', '{Convert.ToInt32(await SecureStorage.GetAsync("id"))}', '{"0,"}')");
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
            current_project = e.SelectedItem as Project_entity;
            ProjectNameLabel.Text += current_project.Name;
            ProjectDescriptionLabel.Text += current_project.Description;
            ProjectCardsLayout.IsVisible = true;
            display.IsVisible = false;
            projectCards.ItemsSource = await GetProjectCards(current_project.CardIds);

        }

        private void CardsBackButton_Clicked(object sender, EventArgs e)
        {
            ProjectNameLabel.Text = "Карточки проекта: ";
            ProjectDescriptionLabel.Text = "Описание проекта: ";
            ProjectCardsLayout.IsVisible = false;
            display.IsVisible = true;
        }

        private async Task<List<Card_entity>> GetProjectCards(string cardIds)
        {
            List<Card_entity> listToReturn = new();
            if (cardIds != "")
            {
                var stringIds = cardIds.Split(',').ToList();
                List<int> ids = new();
                for (int i = 0; i < stringIds.Count; i++)
                {
                    if (stringIds[i] != "")
                        ids.Add(int.Parse(stringIds[i]));
                }

                try
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

                    await using var dataSource = dataSourceBuilder.Build();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var command = dataSource.CreateCommand($"SELECT * FROM card_entities WHERE id='{ids[i]}' LIMIT 1");
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Card_entity card = new();
                            card.Id = (int)reader["id"];
                            card.Name = (string)reader["name"];
                            listToReturn.Add(card);
                        }
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                return listToReturn;
            }
            return new();
        }

        private async void projectCards_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var selected_card = e.SelectedItem as Card_entity;
            if(await DisplayAlert("Подтверждение", "Вы уверены, что хотите удалить эту карточку?", "Да", "Нет"))
            {
                if (await DeleteCard(selected_card!.Id))
                {
                    await DisplayAlert("Успешно", "", "ОК");
                    projectCards.ItemsSource = await GetProjectCards(current_project.CardIds);
                }
                else
                {
                    await DisplayAlert("Ошибка", "При удалении произошла ошибка", "ОК");
                }
            }
        }

        private void AddCardToProjectButton_Clicked(object sender, EventArgs e)
        {
            ProjectCardsLayout.IsVisible = false;
            AddCardLayout.IsVisible = true;
        }

        private async void DeleteProjectButton_Clicked(object sender, EventArgs e)
        {
            if(await DisplayAlert("Подтверждение", "Вы уверены что хотите удалить этот проект?","Да","Нет"))
            {
                if(await DeleteProject(current_project.Id))
                {
                    await DisplayAlert("Успешно", "", "ОК");
                    ProjectCardsLayout.IsVisible = false;
                    display.IsVisible = true;
                    projectListView.ItemsSource = await GetUserProjects(Convert.ToInt32(await SecureStorage.GetAsync("id")));
                    
                }
                else
                {
                    await DisplayAlert("Ошибка", "При удалении произошла ошибка", "ОК");
                }
            }
        }

        private async Task<bool> DeleteProject(int id)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

            await using var dataSource = dataSourceBuilder.Build();
            var command = dataSource.CreateCommand($"DELETE FROM project_entities WHERE id='{id}'");
            if(command.ExecuteNonQuery() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
           
        }

        private async Task<bool> DeleteCard(int id)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

            await using var dataSource = dataSourceBuilder.Build();
            var command = dataSource.CreateCommand($"DELETE FROM card_entities WHERE id='{id}'");
            if (command.ExecuteNonQuery() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private async void AddCardButton_Clicked(object sender, EventArgs e)
        {
            if (await AddCardToProject(current_project.Id, CardNameEntry.Text))
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

                await using var dataSource = dataSourceBuilder.Build();
                await DisplayAlert("Успешно", "", "ОК");
                ProjectCardsLayout.IsVisible = true;
                AddCardLayout.IsVisible = false;
                CardNameEntry.Text = "";
                var command = dataSource.CreateCommand($"SELECT * FROM project_entities WHERE id='{current_project.Id}' LIMIT 1");

                var reader = await command.ExecuteReaderAsync();
                Project_entity project = new();
                while (reader.Read())
                {
                    
                    project.Id = (int)reader["Id"];
                    project.Name = (string)reader["Name"];
                    project.Description = (string)reader["Description"];
                    project.CardIds = (string)reader["CardIds"];
                }
                projectCards.ItemsSource = await GetProjectCards(project.CardIds);
            }
            else
            {
                await DisplayAlert("Ошибка", "При добавлении произошла ошибка", "ОК");
            }
        }

        private async Task<bool> AddCardToProject(int projectId, string cardName)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(DB_conection.conectionstring);

            await using var dataSource = dataSourceBuilder.Build();

            var command = dataSource.CreateCommand($"INSERT INTO card_entities (name)   VALUES ('{cardName}')");
            await command.ExecuteNonQueryAsync();
            command = dataSource.CreateCommand($"SELECT * FROM card_entities ORDER BY id DESC LIMIT 1");
            var reader = await command.ExecuteReaderAsync();
            Card_entity card = new();
            while (reader.Read())
            {
                card.Id = (int)reader["id"];
            }
            command = dataSource.CreateCommand($"UPDATE project_entities SET cardids='{current_project.CardIds + "," + card.Id}' WHERE id='{current_project.Id}'");
            if (await command.ExecuteNonQueryAsync() > 0)
            {
                return true;
            }
            else
                return false;
        }

        private void CardBackButton_Clicked(object sender, EventArgs e)
        {
            ProjectCardsLayout.IsVisible = true;
            AddCardLayout.IsVisible = false;
            CardNameEntry.Text = "";
        }
    }
}
