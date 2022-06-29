using DbmsApi;
using DbmsApi.API;
using DbmsApi.Mongo;
using MathPackage;
using ModelConverter;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdminApp
{
    public partial class Main : Form
    {
        private DBMSAPIController Controller;
        //private string dbmsurl = "https://142.244.110.135/api/";
        private string dbmsurl = "https://localhost:44322//api/";

        private UserData User;
        private Dictionary<string, ModelMetadata> ModelMetadatas;
        private Dictionary<string, CatalogObjectMetadata> CatalogMetadatas;
        private Dictionary<string, Material> Materials;
        private Dictionary<string, UserData> AllUsers;
        private Dictionary<string, ModelMetadata> AllModels;
        private List<string> AllUserNames;

        public Main()
        {
            InitializeComponent();

            Controller = new DBMSAPIController(dbmsurl);
        }

        #region Login/Logout:

        private void Main_Load(object sender, EventArgs e)
        {
            this.Enabled = false;
            LoginForm login = new LoginForm(Controller);
            login.ShowDialog();
            if (login.DialogResult == DialogResult.Cancel)
            {
                this.Close();
            }
            if (login.DialogResult == DialogResult.OK)
            {
                this.Enabled = true;
                this.User = login.User;
                RefreshAsync();

                this.groupBoxCO.Enabled = User.IsAdmin;
                this.groupBoxMaterial.Enabled = User.IsAdmin;
                this.groupBoxUsers.Enabled = User.IsAdmin;
                this.groupBoxAllModels.Enabled = User.IsAdmin;
                this.groupBoxModelDataset.Enabled = User.IsAdmin;
                this.groupBoxCatalogDataset.Enabled = User.IsAdmin;
            }
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            Controller.Logout();
            User = null;
            ResetStuff();
            Main_Load(null, null);
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshAsync();
        }

        private void ResetStuff()
        {
            ModelMetadatas = new Dictionary<string, ModelMetadata>();
            CatalogMetadatas = new Dictionary<string, CatalogObjectMetadata>();
            Materials = new Dictionary<string, Material>();
            AllUsers = new Dictionary<string, UserData>();
            AllModels = new Dictionary<string, ModelMetadata>();

            this.dataGridViewCurrentUserProperties.Rows.Clear();
            this.dataGridViewUserModels.Rows.Clear();
            this.dataGridViewUserModelProperties.Rows.Clear();
            this.dataGridViewCatalogObjects.Rows.Clear();
            this.dataGridViewCatalogPorperties.Rows.Clear();
            this.dataGridViewMaterial.Rows.Clear();
            this.dataGridViewMaterialProperties.Rows.Clear();
            this.dataGridViewUsers.Rows.Clear();
            this.dataGridViewUserProperties.Rows.Clear();
            this.dataGridViewAllModels.Rows.Clear();
            this.dataGridViewAllModelProperties.Rows.Clear();
            this.dataGridViewDatasetCatalog.Rows.Clear();
            this.dataGridViewDatasetModel.Rows.Clear();
        }

        private async Task RefreshAsync()
        {
            ResetStuff();

            await GetTypes();
            SetTypeTree();

            DisplayUserData();
            DisplayAvailableModels();
            DisplayAvailableCatalogObjects();
            DisplayMaterials();
            GetAllUsers();
            if (User.IsAdmin)
            {
                DisplayAllUsers();
                DisplayAllModels();
            }
        }

        private async void GetAllUsers()
        {
            APIResponse<List<string>> response = await Controller.GetUserNames();
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }
            AllUserNames = response.Data;
        }

        private async void buttonUpdateUserData_Click(object sender, EventArgs e)
        {
            User.PublicName = this.textBoxPublicName.Text;
            APIResponse<string> response = await Controller.UpdateUserData(User);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }
            DisplayUserData();
        }

        private async void buttonUpdatePassword_Click(object sender, EventArgs e)
        {
            UpdateUserDataForm updateUserData = new UpdateUserDataForm();
            if (updateUserData.ShowDialog() == DialogResult.OK)
            {
                APIResponse<string> response = await Controller.UpdateUserPassword(updateUserData.AuthModel);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }
            }
            DisplayUserData();
        }

        private void DisplayUserData()
        {
            this.textBoxUsername.Text = User.Username;
            this.textBoxPublicName.Text = User.PublicName;
            this.labelIsAdmin.Visible = User.IsAdmin;

            this.DisplayProperties(this.dataGridViewCurrentUserProperties, User.Properties);
        }

        private void DisplayProperties(DataGridView view, DbmsApi.API.Properties properties)
        {
            view.Rows.Clear();
            foreach (Property property in properties)
            {
                view.Rows.Add(property.Name, property.GetValueString());
            }
        }

        #endregion

        #region Models:

        private async Task DisplayAvailableModels()
        {
            this.dataGridViewUserModels.Rows.Clear();
            ModelMetadatas = new Dictionary<string, ModelMetadata>();
            APIResponse<List<ModelMetadata>> response = await Controller.GetAvailableModels();
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                foreach (ModelMetadata data in response.Data)
                {
                    ModelMetadatas.Add(data.ModelId, data);
                    this.dataGridViewUserModels.Rows.Add(data.Name, data.ModelId, data.Owner, data.ObjectCount);
                }
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            if (User.IsAdmin)
            {
                DisplayAllModels();
                DisplayAllUsers();
            }
        }

        private async void buttonAddModel_Click(object sender, EventArgs e)
        {
            Model modelToAdd = PromptForModel();
            if (modelToAdd == null)
            {
                return;
            }

            APIResponse<string> response = await Controller.CreateModel(modelToAdd);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableModels();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewUserModelProperties, ModelMetadatas[response.Data].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonEditModel_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;
            Model modelToEdit = PromptForModel();
            if (modelToEdit == null)
            {
                return;
            }
            modelToEdit.Id = modelId;
            APIResponse<string> response = await Controller.UpdateModel(modelToEdit);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableModels();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewUserModelProperties, ModelMetadatas[modelToEdit.Id].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonShareModel_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;
            APIResponse<ModelPermission> response = await Controller.GetModelPermissions(modelId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            ModelMetadata selectedModel = ModelMetadatas.Where(u => u.Key == modelId).Select(v => v.Value).FirstOrDefault();
            ModelEditForm modelEditForm = new ModelEditForm(selectedModel, AllUserNames, response.Data);
            if (modelEditForm.ShowDialog() == DialogResult.OK)
            {
                APIResponse<string> response1 = await Controller.ConfigureModelPermissions(modelEditForm.ModelPermission);
                if (response1.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response1.ReasonPhrase);
                }
            }

            await DisplayAvailableModels();
        }

        private async void buttonDownloadModel_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }

            LevelOfDetailForm lodf = new LevelOfDetailForm();
            if (lodf.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;
            APIResponse<Model> response = await Controller.GetModel(new ItemRequest(modelId, lodf.LOD));
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "BIMkit File (*.bpm)|*.bpm|All files (*.*)|*.*";
            sfd.FileName = response.Data.Name + ".bpm";
            if (sfd.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            DBMSReadWrite.WriteModel(response.Data, sfd.FileName);
        }

        private async void buttonExportModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("For exporting to other types such as GBXML. Not implemented");
            return;

            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }
            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;

            LevelOfDetailForm lodf = new LevelOfDetailForm();
            if (lodf.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            DatasetTypeSelectForm dtsf = new DatasetTypeSelectForm();
            if (dtsf.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            string extension = string.Empty;
            string fileData = string.Empty;
            if (dtsf.SelectedDataset == ConverterGeneral.Datasets.GBXML)
            {
                fileData = await GetModelAsGBXML(lodf.LOD, modelId);
                extension = ".xml";
            }

            if (string.IsNullOrWhiteSpace(fileData))
            {
                MessageBox.Show("Could Not Convert File");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "BIMkit File (*.bpm)|*.bpm|All files (*.*)|*.*";
            sfd.FileName = modelId + extension;
            if (sfd.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            File.WriteAllText(sfd.FileName, fileData);
        }

        private async Task<string> GetModelAsGBXML(LevelOfDetail lod, string modelId)
        {
            APIResponse<Model> response = await Controller.GetModel(new ItemRequest(modelId, lod));
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return string.Empty;
            }

            Model model = response.Data;







            return string.Empty;
        }

        private async void buttonDeleteModel_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;

            APIResponse<string> response = await Controller.DeleteModel(modelId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            await DisplayAvailableModels();
        }

        private Model PromptForModel()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "BIMkit File (*.bpm)|*.bpm|All files (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return DBMSReadWrite.ReadModel(ofd.FileName);
        }

        private void dataGridViewModels_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridViewUserModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewUserModels.SelectedRows[0].Cells[1].Value as string;
            this.DisplayProperties(this.dataGridViewUserModelProperties, ModelMetadatas[modelId].Properties);
        }

        #endregion

        #region Catalog Objects:

        private async Task DisplayAvailableCatalogObjects()
        {
            this.dataGridViewCatalogObjects.Rows.Clear();
            this.dataGridViewDatasetCatalog.Rows.Clear();
            CatalogMetadatas = new Dictionary<string, CatalogObjectMetadata>();
            APIResponse<List<CatalogObjectMetadata>> response = await Controller.GetAvailableCatalogObjects();
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                foreach (CatalogObjectMetadata data in response.Data)
                {
                    CatalogMetadatas.Add(data.CatalogObjectId, data);
                    this.dataGridViewCatalogObjects.Rows.Add(data.Name, data.CatalogObjectId, data.Type);
                    this.dataGridViewDatasetCatalog.Rows.Add(data.Name, data.CatalogObjectId, data.Type);
                }
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonAddCatalogObject_Click(object sender, EventArgs e)
        {
            CatalogObject catalogObjectToAdd = PromptForCatalogObj();
            if (catalogObjectToAdd == null)
            {
                return;
            }

            LevelOfDetailForm lodForm = new LevelOfDetailForm();
            if (lodForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            MeshRep newMesh = new MeshRep()
            {
                Components = catalogObjectToAdd.Components,
                Joints = new List<Joint>(),
                LevelOfDetail = lodForm.LOD
            };
            MongoCatalogObject mongoCO = new MongoCatalogObject()
            {
                Id = null,
                MeshReps = new List<MeshRep>() { newMesh },
                Name = catalogObjectToAdd.Name,
                Properties = catalogObjectToAdd.Properties,
                TypeId = catalogObjectToAdd.TypeId
            };

            APIResponse<string> response = await Controller.CreateCatalogObject(mongoCO);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableCatalogObjects();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewCatalogPorperties, CatalogMetadatas[response.Data].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonEditCatalogObject_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewCatalogObjects.SelectedRows.Count != 1)
            {
                return;
            }

            string colId = this.dataGridViewCatalogObjects.SelectedRows[0].Cells[1].Value as string;
            CatalogObject catalogObjectToAdd = PromptForCatalogObj();
            if (catalogObjectToAdd == null)
            {
                return;
            }

            LevelOfDetailForm lodForm = new LevelOfDetailForm();
            if (lodForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            MeshRep newMesh = new MeshRep()
            {
                Components = catalogObjectToAdd.Components,
                Joints = new List<Joint>(),
                LevelOfDetail = lodForm.LOD
            };
            MongoCatalogObject mongoCO = new MongoCatalogObject()
            {
                Id = colId,
                MeshReps = new List<MeshRep>() { newMesh },
                Name = catalogObjectToAdd.Name,
                Properties = catalogObjectToAdd.Properties,
                TypeId = catalogObjectToAdd.TypeId
            };
            APIResponse<string> response = await Controller.UpdateCatalogObject(mongoCO);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableCatalogObjects();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewCatalogPorperties, CatalogMetadatas[mongoCO.Id].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonType_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewCatalogObjects.SelectedRows.Count != 1)
            {
                return;
            }

            string colId = this.dataGridViewCatalogObjects.SelectedRows[0].Cells[1].Value as string;
            CatalogObjectMetadata com = CatalogMetadatas[colId];

            TypeChangeForm tcf = new TypeChangeForm(ObjectTypeTree.GetAllTypes());
            if (tcf.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            com.Type = tcf.Type;

            APIResponse<string> response = await Controller.UpdateCatalogObjectMetaData(com);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableCatalogObjects();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewCatalogPorperties, CatalogMetadatas[colId].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }

        }

        private async void buttonDeleteCatalogObject_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewCatalogObjects.SelectedRows.Count != 1)
            {
                return;
            }

            string colId = this.dataGridViewCatalogObjects.SelectedRows[0].Cells[1].Value as string;

            APIResponse<string> response = await Controller.DeleteCatalogObject(colId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            await DisplayAvailableCatalogObjects();
        }

        private CatalogObject PromptForCatalogObj()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "BIMkit File (*.bpo)|*.bpo|All files (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return DBMSReadWrite.ReadCatalogObject(ofd.FileName);
        }

        private void dataGridViewCatalogObjects_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridViewCatalogObjects.SelectedRows.Count != 1)
            {
                return;
            }

            string colId = this.dataGridViewCatalogObjects.SelectedRows[0].Cells[1].Value as string;
            this.DisplayProperties(this.dataGridViewCatalogPorperties, CatalogMetadatas[colId].Properties);
        }

        #endregion

        #region Materials:

        private async Task DisplayMaterials()
        {
            this.dataGridViewMaterial.Rows.Clear();
            Materials = new Dictionary<string, Material>();
            APIResponse<List<Material>> response = await Controller.GetMaterials();
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                foreach (Material data in response.Data)
                {
                    Materials.Add(data.Id, data);
                    this.dataGridViewMaterial.Rows.Add(data.Name, data.Id);
                }
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonAddMaterial_Click(object sender, EventArgs e)
        {
            MaterialForm matForm = new MaterialForm();
            if (matForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Material newMat = new Material()
            {
                Name = matForm.Name,
                Properties = new DbmsApi.API.Properties()
            };
            APIResponse<string> response = await Controller.CreateMaterial(newMat);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayMaterials();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewMaterialProperties, Materials[response.Data].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonEditMaterial_Click(object sender, EventArgs e)
        {
            MaterialForm matForm = new MaterialForm();
            if (matForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (this.dataGridViewMaterial.SelectedRows.Count != 1)
            {
                return;
            }

            string matlId = this.dataGridViewMaterial.SelectedRows[0].Cells[1].Value as string;
            Material newMat = new Material()
            {
                Id = matlId,
                Name = matForm.Name,
                Properties = new DbmsApi.API.Properties()
            };
            APIResponse<string> response = await Controller.UpdateMaterial(newMat);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayMaterials();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewMaterialProperties, Materials[response.Data].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonDeleteMaterial_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewMaterial.SelectedRows.Count != 1)
            {
                return;
            }

            string matlId = this.dataGridViewMaterial.SelectedRows[0].Cells[1].Value as string;

            APIResponse<string> response = await Controller.DeleteMaterial(matlId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            await DisplayMaterials();
        }

        private void dataGridViewMaterial_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridViewMaterial.SelectedRows.Count != 1)
            {
                return;
            }

            string matlId = this.dataGridViewMaterial.SelectedRows[0].Cells[1].Value as string;
            this.DisplayProperties(this.dataGridViewMaterialProperties, Materials[matlId].Properties);
        }

        #endregion

        #region All Users

        private async Task DisplayAllUsers()
        {
            this.dataGridViewUsers.Rows.Clear();
            AllUsers = new Dictionary<string, UserData>();
            APIResponse<List<UserData>> response = await Controller.GetUserList();
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                foreach (UserData data in response.Data)
                {
                    AllUsers.Add(data.Username, data);
                    this.dataGridViewUsers.Rows.Add(data.PublicName, data.Username, data.IsAdmin);
                }
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonAddUser_Click(object sender, EventArgs e)
        {
            UserData newUserData = new UserData()
            {
                AccessibleModels = new List<string>(),
                IsAdmin = false,
                OwnedModels = new List<string>(),
                Properties = new DbmsApi.API.Properties(),
                PublicName = "",
                Username = "newuser"
            };
            AdminUserEditForm adminUserEditForm = new AdminUserEditForm(newUserData, true, AllModels.Values.ToList());
            if (adminUserEditForm.ShowDialog() == DialogResult.OK)
            {
                NewUser newUser = new NewUser(adminUserEditForm.UserData.Username, adminUserEditForm.UserData.PublicName, adminUserEditForm.NewPassword);
                APIResponse<UserData> response = await Controller.CreateUser(newUser);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                await DisplayAllUsers();
            }
        }

        private async void buttonEditUser_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUsers.SelectedRows.Count != 1)
            {
                return;
            }

            string userId = this.dataGridViewUsers.SelectedRows[0].Cells[1].Value as string;
            UserData selectedUser = AllUsers.Where(u => u.Key == userId).Select(v => v.Value).FirstOrDefault();

            AdminUserEditForm adminUserEditForm = new AdminUserEditForm(selectedUser, false, AllModels.Values.ToList());
            if (adminUserEditForm.ShowDialog() == DialogResult.OK)
            {
                APIResponse<string> response = await Controller.UpdateUserData(adminUserEditForm.UserData);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                if (!string.IsNullOrWhiteSpace(adminUserEditForm.NewPassword))
                {
                    AuthModel authModel = new AuthModel()
                    {
                        Username = adminUserEditForm.UserData.Username,
                        Password = adminUserEditForm.NewPassword
                    };
                    APIResponse<string> response2 = await Controller.UpdateUserPassword(authModel);
                    if (response2.Code != System.Net.HttpStatusCode.OK)
                    {
                        MessageBox.Show(response2.ReasonPhrase);
                    }
                }

                DisplayAllUsers();
                DisplayAvailableModels();
            }
        }

        private async void buttonDeleteUser_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewUsers.SelectedRows.Count != 1)
            {
                return;
            }

            string userId = this.dataGridViewUsers.SelectedRows[0].Cells[1].Value as string;
            if (userId == User.Username)
            {
                MessageBox.Show("Cannot delete yourself, sorry.");
                return;
            }

            APIResponse<string> response = await Controller.DeleteUser(userId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            DisplayAllUsers();
            DisplayAvailableModels();
        }

        private void dataGridViewUserProperties_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridViewUsers.SelectedRows.Count != 1)
            {
                return;
            }

            string userId = this.dataGridViewUsers.SelectedRows[0].Cells[1].Value as string;
            this.DisplayProperties(this.dataGridViewUserProperties, AllUsers[userId].Properties);
        }

        #endregion

        #region All Models

        private async Task DisplayAllModels()
        {
            this.dataGridViewAllModels.Rows.Clear();
            this.dataGridViewDatasetModel.Rows.Clear();
            AllModels = new Dictionary<string, ModelMetadata>();
            APIResponse<List<ModelMetadata>> response = await Controller.GetAllModels();
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                foreach (ModelMetadata data in response.Data)
                {
                    AllModels.Add(data.ModelId, data);
                    this.dataGridViewAllModels.Rows.Add(data.Name, data.ModelId, data.Owner, data.ObjectCount);
                    this.dataGridViewDatasetModel.Rows.Add(data.Name, data.ModelId, data.Owner, data.ObjectCount);
                }
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }

        private async void buttonEditModels_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewAllModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewAllModels.SelectedRows[0].Cells[1].Value as string;
            APIResponse<ModelPermission> response = await Controller.GetModelPermissions(modelId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            ModelMetadata selectedModel = AllModels.Where(u => u.Key == modelId).Select(v => v.Value).FirstOrDefault();
            ModelEditForm modelEditForm = new ModelEditForm(selectedModel, AllUserNames, response.Data);
            if (modelEditForm.ShowDialog() == DialogResult.OK)
            {
                APIResponse<string> response1 = await Controller.ConfigureModelPermissions(modelEditForm.ModelPermission);
                if (response1.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response1.ReasonPhrase);
                }

                await DisplayAllModels();
            }
        }

        private async void buttonDeleteModels_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewAllModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewAllModels.SelectedRows[0].Cells[1].Value as string;
            APIResponse<string> response = await Controller.DeleteModel(modelId);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
            }

            await DisplayAllModels();
        }

        private void dataGridViewModelsProperties_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridViewAllModels.SelectedRows.Count != 1)
            {
                return;
            }

            string modelId = this.dataGridViewAllModels.SelectedRows[0].Cells[1].Value as string;
            this.DisplayProperties(this.dataGridViewAllModelProperties, AllModels[modelId].Properties);
        }

        #endregion

        #region Types

        private async Task GetTypes()
        {
            APIResponse<List<ObjectType>> response3 = await Controller.GetTypes();
            if (response3.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response3.ReasonPhrase);
                return;
            }
            ObjectTypeTree.BuildTypeTree(response3.Data);
        }

        private void SetTypeTree()
        {
            // Empty treeview
            this.treeViewTypes.Nodes.Clear();

            // Get type tree
            ObjectType ttnRoot = ObjectTypeTree.GetType("Root");

            // Create matching root node
            TreeNode tnRoot = new TreeNode(ttnRoot.Name.ToString());
            tnRoot.Tag = ttnRoot;
            this.treeViewTypes.Nodes.Add(tnRoot);

            // Recursively create matching treenodes
            void generateRecursive(ObjectType ttn, TreeNode tn)
            {
                foreach (ObjectType _ttn in ObjectTypeTree.GetTypeChildren(ttn.Name))
                {
                    var _tn = new TreeNode(_ttn.Name.ToString());
                    _tn.Tag = _ttn;
                    tn.Nodes.Add(_tn);
                    generateRecursive(_ttn, _tn);
                }
            }

            generateRecursive(ttnRoot, tnRoot);

            // Expand the tree
            tnRoot.ExpandAll();
        }

        private async void buttonAddType_Click(object sender, EventArgs e)
        {
            // Get the new Type
            TypeEditForm tef = new TypeEditForm(ObjectTypeTree.GetAllTypes());
            if (tef.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            APIResponse<string> response = await Controller.CreateType(tef.Type);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            RefreshAsync();
        }

        private async void buttonEditType_Click(object sender, EventArgs e)
        {
            // Get the Type
            ObjectType selectedType = (ObjectType) this.treeViewTypes.SelectedNode.Tag;

            TypeEditForm tef = new TypeEditForm(selectedType, ObjectTypeTree.GetAllTypes());
            if (tef.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            APIResponse<string> response = await Controller.UpdateType(tef.Type);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            RefreshAsync();
        }

        private async void buttonDeleteType_Click(object sender, EventArgs e)
        {
            // Get the Type
            ObjectType selectedType = (ObjectType)this.treeViewTypes.SelectedNode.Tag;

            if (MessageBox.Show("Are you sure?", "Deleting: " + selectedType.Name, MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            APIResponse<string> response = await Controller.DeleteType(selectedType.Name);
            if (response.Code != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(response.ReasonPhrase);
                return;
            }

            RefreshAsync();
        }

        #endregion

        #region Dataset Adding:

        public static int BULK_UPLOAD_LIMIT = 10;

        private async void buttonBulkAddModel_Click(object sender, EventArgs e)
        {
            DatasetTypeSelectForm datasetTypeSelect = new DatasetTypeSelectForm();
            if (datasetTypeSelect.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Promt for the file:
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (datasetTypeSelect.SelectedDataset == ConverterGeneral.Datasets._3DFRONT)
            {
                GetModelsFrom3DFRONTDataset(fbd.SelectedPath);
            }
            if (datasetTypeSelect.SelectedDataset == ConverterGeneral.Datasets.COOHOM)
            {
                GetModelsFromCoohomDataset(fbd.SelectedPath);
            }
        }

        public async void GetModelsFrom3DFRONTDataset(string path)
        {
            // Assume that the Folder names in the Main folder are labeled
            string[] files = Directory.GetFiles(path, "*.json");

            int counter = 0;
            foreach (string file in files)
            {
                string modelName = Path.GetFileNameWithoutExtension(file);
                if (ModelMetadatas.Values.Any(v => v.Name == modelName))
                {
                    continue;
                }
                Model modelToAdd;
                try
                {
                    modelToAdd = ModelConverter.DatasetConverter3DFRONT.Convert3DFRONTModel(file, 1.0, true, true);
                }
                catch
                {
                    MessageBox.Show("Error with file:\n" + file);
                    continue;
                }
                if (ModelMetadatas.Values.Any(v => v.Name == modelToAdd.Name))
                {
                    continue;
                }

                APIResponse<string> response = await Controller.CreateModel(modelToAdd);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                counter++;
                if (counter == BULK_UPLOAD_LIMIT)
                {
                    break;
                }
            }

            await DisplayAllModels();
        }

        public async void GetModelsFromCoohomDataset(string path)
        {
            // Assume that the Folder names in the Main folder are labeled
            string[] files = Directory.GetDirectories(path);

            int counter = 0;
            foreach (string file in files)
            {
                string modelName = Path.GetFileNameWithoutExtension(file);
                if (ModelMetadatas.Values.Any(v => v.Name == modelName))
                {
                    continue;
                }
                Model modelToAdd;
                modelToAdd = DatasetConverterCoohom.ConvertCoohomModel(file, Utils.MM_TO_METER, true, false);
                if (ModelMetadatas.Values.Any(v => v.Name == modelToAdd.Name))
                {
                    continue;
                }

                APIResponse<string> response = await Controller.CreateModel(modelToAdd);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                counter++;
                if (counter == BULK_UPLOAD_LIMIT)
                {
                    break;
                }
            }

            await DisplayAllModels();
        }

        private async void buttonBulkDeleteModel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("INCOMPLETE");
        }

        private async void buttonBulkAddCatalog_Click(object sender, EventArgs e)
        {
            DatasetTypeSelectForm datasetTypeSelect = new DatasetTypeSelectForm();
            if (datasetTypeSelect.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Promt for the file:
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            // Should state what format the dataset is from:
            if (datasetTypeSelect.SelectedDataset == ConverterGeneral.Datasets._3DFRONT)
            {
                GetObjectsFrom3DFRONTDataset(fbd.SelectedPath);
            }
            if (datasetTypeSelect.SelectedDataset == ConverterGeneral.Datasets.COOHOM)
            {
                GetObjectsFromCoohomDataset(fbd.SelectedPath);
            }
        }

        public async void GetObjectsFrom3DFRONTDataset(string path)
        {
            // Import the Types from model_info.json:
            List<Dataset3DFRONTClasses.Type> typeList = DBMSReadWrite.JSONReadFromFile<List<Dataset3DFRONTClasses.Type>>(path + "\\model_info.json");

            // Assume that the Folder names in the Main folder are labeled
            string[] dirs = Directory.GetDirectories(path);
            int counter = 0;
            foreach (string dir in dirs)
            {
                string objName = Path.GetFileNameWithoutExtension(dir);
                if (CatalogMetadatas.Values.Any(v => v.Name == objName))
                {
                    continue;
                }
                CatalogObject newObj;
                try
                {
                    newObj = DatasetConverter3DFRONT.Convert3DFRONTCatalogObject(dir, 1.0, false, true);
                }
                catch
                {
                    MessageBox.Show("Error with file:\n" + dir);
                    continue;
                }
                if (CatalogMetadatas.Values.Any(v => v.Name == newObj.Name))
                {
                    continue;
                }

                Dataset3DFRONTClasses.Type type = typeList.First(t => t.model_id == newObj.CatalogID);
                newObj.TypeId = type.category + "_" + type.supercategory;

                MongoCatalogObject mongoCO = new MongoCatalogObject()
                {
                    Id = newObj.CatalogID,
                    MeshReps = new List<MeshRep>() { new MeshRep() { Components = newObj.Components ,LevelOfDetail = LevelOfDetail.LOD500} },
                    Name = newObj.Name,
                    Properties = newObj.Properties,
                    TypeId = newObj.TypeId,
                    Tags = newObj.Tags
                };

                APIResponse<string> response = await Controller.CreateCatalogObject(mongoCO);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                counter++;
                if (counter == BULK_UPLOAD_LIMIT)
                {
                    break;
                }
            }

            await DisplayAvailableCatalogObjects();
        }

        public async void GetObjectsFromCoohomDataset(string path)
        {
            // Assume that the Folder names in the Main folder are labeled
            string[] dirs = Directory.GetDirectories(path);
            int counter = 0;
            foreach (string dir in dirs)
            {
                string objName = Path.GetFileNameWithoutExtension(dir);
                if (CatalogMetadatas.Values.Any(v => v.Name == objName))
                {
                    continue;
                }
                CatalogObject newObj;
                try
                {
                    newObj = DatasetConverterCoohom.ConvertCoohomCatalogObject(dir, Utils.MM_TO_METER, false, false);
                }
                catch
                {
                    MessageBox.Show("Error with file:\n" + dir);
                    continue;
                }
                if (CatalogMetadatas.Values.Any(v => v.Name == newObj.Name))
                {
                    continue;
                }

                MongoCatalogObject mongoCO = new MongoCatalogObject()
                {
                    Id = newObj.CatalogID,
                    MeshReps = new List<MeshRep>() { new MeshRep() { Components = newObj.Components, LevelOfDetail = LevelOfDetail.LOD500 } },
                    Name = newObj.Name,
                    Properties = newObj.Properties,
                    TypeId = newObj.TypeId,
                    Tags = newObj.Tags
                };

                APIResponse<string> response = await Controller.CreateCatalogObject(mongoCO);
                if (response.Code != System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show(response.ReasonPhrase);
                }

                counter++;
                if (counter == BULK_UPLOAD_LIMIT)
                {
                    break;
                }
            }

            await DisplayAvailableCatalogObjects();
        }

        private async void buttonBulkDeleteCatalog_Click(object sender, EventArgs e)
        {
            MessageBox.Show("INCOMPLETE");
        }

        private void numericUpDownUploadLimit_ValueChanged(object sender, EventArgs e)
        {
            BULK_UPLOAD_LIMIT = Convert.ToInt32(this.numericUpDownUploadLimit.Value);
        }

        #endregion

        private async void buttonTestItemAdd_Click(object sender, EventArgs e)
        {
            TestBoxObjectForm tbof = new TestBoxObjectForm(ObjectTypeTree.GetAllTypes());
            if (tbof.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            APIResponse<string> response = await Controller.CreateCatalogObject(tbof.MongoCatalogObject);
            if (response.Code == System.Net.HttpStatusCode.OK)
            {
                await DisplayAvailableCatalogObjects();

                // Highlight and show the model info
                this.DisplayProperties(this.dataGridViewCatalogPorperties, CatalogMetadatas[response.Data].Properties);
            }
            else
            {
                MessageBox.Show(response.ReasonPhrase);
            }
        }
    }
}