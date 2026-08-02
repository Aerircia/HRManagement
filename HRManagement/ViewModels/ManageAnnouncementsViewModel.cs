using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Manage Announcements" page - Admin-only CRUD over the announcement
    /// board shown on DashboardView. Follows the same Add/Edit + Delete
    /// confirmation overlay pattern as ManageContractsViewModel /
    /// ManageProfilesViewModel so it looks and behaves consistently with
    /// the rest of the Manage* pages. All persistence/authorization/logging
    /// lives in IManageAnnouncementsService; this ViewModel only owns
    /// presentation state (form fields, row mapping, filtering).
    /// </summary>
    public class ManageAnnouncementsViewModel : PageViewModel
    {
        private readonly IManageAnnouncementsService _manageAnnouncementsService;

        public override string Title => "Manage Announcements";

        public ManageAnnouncementsViewModel(
            IManageAnnouncementsService manageAnnouncementsService)
        {
            _manageAnnouncementsService = manageAnnouncementsService;

            Announcements = [];

            AnnouncementsView = CollectionViewSource.GetDefaultView(Announcements);
            AnnouncementsView.Filter = FilterAnnouncement;

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as AnnouncementRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as AnnouncementRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            HasAccess = _manageAnnouncementsService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
                LoadAnnouncements();
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // List

        public ObservableCollection<AnnouncementRow> Announcements { get; }
        public ICollectionView AnnouncementsView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    AnnouncementsView.Refresh();
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

        private string _formTitleText = "Add Announcement";
        public string FormTitleText
        {
            get => _formTitleText;
            set => SetProperty(ref _formTitleText, value);
        }

        private int _formAnnouncementId;

        private string _formTitle = string.Empty;
        public string FormTitle
        {
            get => _formTitle;
            set => SetProperty(ref _formTitle, value);
        }

        private string _formContent = string.Empty;
        public string FormContent
        {
            get => _formContent;
            set => SetProperty(ref _formContent, value);
        }

        private bool _formIsActive = true;
        public bool FormIsActive
        {
            get => _formIsActive;
            set => SetProperty(ref _formIsActive, value);
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

        private AnnouncementRow? _pendingDelete;
        public AnnouncementRow? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

        private void LoadAnnouncements()
        {
            Announcements.Clear();
            foreach (var announcement in _manageAnnouncementsService.GetAnnouncements())
                Announcements.Add(ToRow(announcement));
        }

        private static AnnouncementRow ToRow(Announcement announcement)
        {
            return new AnnouncementRow
            {
                Announcement = announcement,
                Title = announcement.Title,
                Content = announcement.Content,
                PostedByName = announcement.PostedByName,
                PostedDateDisplay = announcement.PostedDate.ToString("MMM dd, yyyy"),
                Status = announcement.IsActive ? "Active" : "Inactive"
            };
        }

        private bool FilterAnnouncement(object obj)
        {
            if (obj is not AnnouncementRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.Content.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formAnnouncementId = 0;
            FormTitleText = "Add Announcement";
            FormTitle = string.Empty;
            FormContent = string.Empty;
            FormIsActive = true;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(AnnouncementRow? row)
        {
            if (row?.Announcement == null)
                return;

            var announcement = row.Announcement;

            _formAnnouncementId = announcement.AnnouncementId;
            FormTitleText = "Edit Announcement";
            FormTitle = announcement.Title;
            FormContent = announcement.Content;
            FormIsActive = announcement.IsActive;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        private void SaveForm()
        {
            var input = new AnnouncementInput
            {
                AnnouncementId = _formAnnouncementId,
                Title = FormTitle,
                Content = FormContent,
                IsActive = FormIsActive
            };

            try
            {
                var savedAnnouncement = _manageAnnouncementsService.SaveAnnouncement(input);

                if (_formAnnouncementId == 0)
                {
                    Announcements.Insert(0, ToRow(savedAnnouncement));
                }
                else
                {
                    var existing = Announcements.FirstOrDefault(
                        a => a.Announcement.AnnouncementId == _formAnnouncementId);

                    if (existing != null)
                    {
                        var index = Announcements.IndexOf(existing);
                        Announcements[index] = ToRow(savedAnnouncement);
                    }
                }

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

        private void RequestDelete(AnnouncementRow? row)
        {
            if (row == null)
                return;

            PendingDelete = row;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            if (PendingDelete == null)
                return;

            _manageAnnouncementsService.DeleteAnnouncement(
                PendingDelete.Announcement.AnnouncementId,
                PendingDelete.Title);

            Announcements.Remove(PendingDelete);
            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }
    }


}
