using AntDesign;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MyProject.Business.Services.DataAccess;
using MyProject.Models.AdapterModel;
using MyProject.Models.Systems;
using MyProject.Share.Helpers;

namespace MyProject.Web.Components.Views.Admins
{
    public partial class MyUserView
    {
        private readonly ILogger<MyUserView> logger;
        private readonly MyUserService myUserService;
        private readonly ModalService modalService;
        private readonly MessageService messageService;
        private readonly NotificationService notificationService;
        ITable table;
        int _pageIndex = 1;
        int _pageSize = MagicObjectHelper.PageSize;
        int _total = 0;
        string searchText = string.Empty;
        string sortField = nameof(MyUserAdapterModel.Name);
        string sortDirection = "Ascending";

        List<MyUserAdapterModel> myUserAdapterModels = new();

        string modalTitle = "使用者列表";
        bool modalVisible = false;
        MyUserAdapterModel CurrentRecord = new();
        public EditContext LocalEditContext { get; set; }
        bool isNewRecordMode;

        public MyUserView(ILogger<MyUserView> logger,
            MyUserService myUserService,
            ModalService modalService, MessageService messageService, NotificationService notificationService)
        {
            this.logger = logger;
            this.myUserService = myUserService;
            this.modalService = modalService;
            this.messageService = messageService;
            this.notificationService = notificationService;
        }

        public async Task ReloadAsync()
        {
            DataRequestResult<MyUserAdapterModel> dataRequestResult = await myUserService.GetAsync(new DataRequest
            {
                Search = searchText,
                SortField = sortField,
                SortDescending = sortDirection == "Descending",
                CurrentPage = _pageIndex,
                PageSize = _pageSize,
                Take = 0,
            });

            myUserAdapterModels = dataRequestResult.Result.ToList();
            _total = dataRequestResult.Count;

            StateHasChanged();
        }

        async Task OnTableChange(AntDesign.TableModels.QueryModel<MyUserAdapterModel> args)
        {
            _pageIndex = args.PageIndex;

            await ReloadAsync();
        }

        async Task OnSortAsync()
        {
            _pageIndex = 1;
            await ReloadAsync();
        }

        async Task OnSearchAsync()
        {
            _pageIndex = 1;
            await ReloadAsync();
        }

        async Task OnRefreshAsync()
        {
            await ReloadAsync();

            _ = notificationService.Open(new NotificationConfig()
            {
                Message = "系統訊息",
                Description = "已經更新到最新資料庫紀錄",
                NotificationType = NotificationType.Warning,
                Placement = NotificationPlacement.BottomRight
            });
        }

        async Task OnEditAsync(MyUserAdapterModel myUserAdapterModel)
        {
            isNewRecordMode = false;
            CurrentRecord = myUserAdapterModel.Clone();
            CurrentRecord.Password = string.Empty;
            modalVisible = true;
        }

        async Task OnDeleteAsync(MyUserAdapterModel myUserAdapterModel)
        {
            var ok = await modalService.ConfirmAsync(new ConfirmOptions()
            {
                Title = "確認刪除",
                Content = "確定要刪除這筆紀錄嗎？此操作無法復原。",
                OkText = "刪除",
                CancelText = "取消",
                OkButtonProps = new ButtonProps { Danger = true },
                MaskClosable = false
            });

            if (ok)
            {
                await myUserService.DeleteAsync(myUserAdapterModel.Id);

                _ = notificationService.Open(new NotificationConfig()
                {
                    Message = "系統訊息",
                    Description = "刪除成功",
                    NotificationType = NotificationType.Warning,
                    Placement = NotificationPlacement.BottomRight
                });

                await ReloadAsync();
            }
            else
            {
                // 使用者按「取消」或關閉
            }
        }

        async Task OnAddAsync(bool continueOnCapturedContext)
        {
            CurrentRecord = new();
            isNewRecordMode = true;
            modalVisible = true;
        }

        private async Task OnModalOKHandleAsync(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            #region 進行 Form Validation 檢查驗證作業
            // 編輯模式下密碼為選填，暫時填入佔位值以通過 Required 驗證
            string savedPassword = CurrentRecord.Password;
            if (!isNewRecordMode && string.IsNullOrWhiteSpace(CurrentRecord.Password))
            {
                CurrentRecord.Password = "KEEP_EXISTING";
            }

            if (LocalEditContext.Validate() == false)
            {
                CurrentRecord.Password = savedPassword;

                // 取得所有驗證失敗的錯誤訊息
                IEnumerable<string> allErrors = LocalEditContext.GetValidationMessages();

                foreach (var error in allErrors)
                {
                    _ = notificationService.Open(new NotificationConfig()
                    {
                        Message = "驗證失敗，請修正以下錯誤",
                        Description = error,
                        NotificationType = NotificationType.Error,
                        Placement = NotificationPlacement.BottomRight,
                        Duration = 5
                    });
                }

                modalVisible = true;
                return;
            }

            CurrentRecord.Password = savedPassword;
            #endregion

            #region 新增與修改儲存紀錄
            if (isNewRecordMode == true)
            {
                #region 新增紀錄

                await myUserService.AddAsync(CurrentRecord);

                _ = notificationService.Open(new NotificationConfig()
                {
                    Message = "系統訊息",
                    Description = "新增成功",
                    NotificationType = NotificationType.Warning,
                    Placement = NotificationPlacement.BottomRight
                });

                _ = messageService.SuccessAsync("新增成功");

                await ReloadAsync();

                modalVisible = false;

                #endregion
            }
            else
            {
                #region 修改紀錄
                await myUserService.UpdateAsync(CurrentRecord);

                _ = notificationService.Open(new NotificationConfig()
                {
                    Message = "系統訊息",
                    Description = "修改成功",
                    NotificationType = NotificationType.Warning,
                    Placement = NotificationPlacement.BottomRight
                });

                await ReloadAsync();

                modalVisible = false;
                #endregion
            }
            #endregion

            modalVisible = false;
        }

        private async Task OnModalCancelHandleAsync(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            modalVisible = false;
        }

        private async Task OnModalKeyDownAsync(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await OnModalOKHandleAsync(new MouseEventArgs());
            }
            else if (args.Key == "Escape" || args.Key == "Esc")
            {
                await OnModalCancelHandleAsync(new MouseEventArgs());
            }
        }

        public void OnEditContestChanged(EditContext context)
        {
            LocalEditContext = context;
        }
    }
}
