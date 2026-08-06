using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Manage Department" page - Admin-only CRUD over the Department table.
    /// Follows the same Add/Edit + Delete confirmation overlay pattern as
    /// ManageAnnouncementsViewModel so it looks and behaves consistently
    /// with the rest of the Manage* pages. All persistence/authorization/
    /// logging lives in IManageDepartmentsService; this ViewModel only owns
    /// presentation state (form fields, row mapping, filtering).
    /// </summary>
    public class ManageDepartmentsViewModel : PageViewModel
    {
        private readonly IManageDepartmentsService _manageDepartmentsService;

        public override string Title => "Manage Department";

        public ManageDepartmentsViewModel(
            IManageDepartmentsService manageDepartmentsService)
        {
            _manageDepartmentsService = manageDepartmentsService;

            Departments = [];

            DepartmentsView = CollectionViewSource.GetDefaultView(Departments);
            DepartmentsView.Filter = FilterDepartment;

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as DepartmentRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as DepartmentRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            HasAccess = _manageDepartmentsService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
                LoadDepartments();
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // List

        public ObservableCollection<DepartmentRow> Departments { get; }
        public ICollectionView DepartmentsView { get; }

        public bool IsEmpty => Departments.Count == 0;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    DepartmentsView.Refresh();
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        // Add/Edit form overlay

        private bool _isFormOpen;
        public bool IsFormOpen
        {
            get => _isFormOpen;
            set => SetProperty(ref _isFormOpen, value);
        }

        private string _formTitleText = "Add Department";
        public string FormTitleText
        {
            get => _formTitleText;
            set => SetProperty(ref _formTitleText, value);
        }

        private int _formDepartmentId;

        private string _formDepartmentName = string.Empty;
        public string FormDepartmentName
        {
            get => _formDepartmentName;
            set => SetProperty(ref _formDepartmentName, value);
        }

        private string? _formErrorMessage;
        public string? FormErrorMessage
        {
            get => _formErrorMessage;
            set
            {
                if (SetProperty(ref _formErrorMessage, value))
                    OnPropertyChanged(nameof(HasFormError));
            }
        }

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        // Delete confirmation overlay

        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen
        {
            get => _isDeleteConfirmOpen;
            set => SetProperty(ref _isDeleteConfirmOpen, value);
        }

        private DepartmentRow? _pendingDelete;
        public DepartmentRow? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        private string? _deleteErrorMessage;
        public string? DeleteErrorMessage
        {
            get => _deleteErrorMessage;
            set
            {
                if (SetProperty(ref _deleteErrorMessage, value))
                    OnPropertyChanged(nameof(HasDeleteError));
            }
        }

        public bool HasDeleteError => !string.IsNullOrWhiteSpace(DeleteErrorMessage);

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

        private void LoadDepartments()
        {
            Departments.Clear();
            foreach (var department in _manageDepartmentsService.GetDepartments())
                Departments.Add(department);

            OnPropertyChanged(nameof(IsEmpty));
        }

        private bool FilterDepartment(object obj)
        {
            if (obj is not DepartmentRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.DepartmentName.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formDepartmentId = 0;
            FormTitleText = "Add Department";
            FormDepartmentName = string.Empty;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(DepartmentRow? row)
        {
            if (row?.Department == null)
                return;

            var department = row.Department;

            _formDepartmentId = department.DepartmentId;
            FormTitleText = "Edit Department";
            FormDepartmentName = department.DepartmentName;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        private void SaveForm()
        {
            var input = new DepartmentInput
            {
                DepartmentId = _formDepartmentId,
                DepartmentName = FormDepartmentName
            };

            try
            {
                var savedDepartment = _manageDepartmentsService.SaveDepartment(input);

                if (_formDepartmentId == 0)
                {
                    Departments.Add(savedDepartment);
                }
                else
                {
                    var existing = Departments.FirstOrDefault(
                        d => d.Department.DepartmentId == _formDepartmentId);

                    if (existing != null)
                    {
                        var index = Departments.IndexOf(existing);
                        Departments[index] = savedDepartment;
                    }
                }

                OnPropertyChanged(nameof(IsEmpty));
                IsFormOpen = false;
            }
            catch (ArgumentException exception)
            {
                FormErrorMessage = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                FormErrorMessage = exception.Message;
            }
        }

        // Delete

        private void RequestDelete(DepartmentRow? row)
        {
            if (row == null)
                return;

            PendingDelete = row;
            DeleteErrorMessage = null;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            if (PendingDelete == null)
                return;

            try
            {
                _manageDepartmentsService.DeleteDepartment(
                    PendingDelete.Department.DepartmentId,
                    PendingDelete.DepartmentName);

                Departments.Remove(PendingDelete);
                OnPropertyChanged(nameof(IsEmpty));
                PendingDelete = null;
                IsDeleteConfirmOpen = false;
            }
            catch (InvalidOperationException exception)
            {
                DeleteErrorMessage = exception.Message;
            }
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            DeleteErrorMessage = null;
            IsDeleteConfirmOpen = false;
        }
    }
}
