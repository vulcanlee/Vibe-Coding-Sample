using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.AccessDatas;
using MyProject.AccessDatas.Models;
using MyProject.Business.Factories;
using MyProject.Business.Helpers;
using MyProject.Models.AdapterModel;
using MyProject.Models.Systems;

namespace MyProject.Business.Services.DataAccess;

public class MyUserService
{
    #region 欄位與屬性
    private readonly BackendDBContext context;

    public IMapper Mapper { get; }
    public ILogger<MyUserService> Logger { get; }
    #endregion

    #region 建構式
    public MyUserService(BackendDBContext context, IMapper mapper,
        ILogger<MyUserService> logger)
    {
        this.context = context;
        Mapper = mapper;
        Logger = logger;
    }
    #endregion

    #region CRUD 服務
    public async Task<DataRequestResult<MyUserAdapterModel>> GetAsync(DataRequest dataRequest)
    {
        List<MyUserAdapterModel> data = new();
        DataRequestResult<MyUserAdapterModel> result = new();
        var DataSource = context.MyUser
            .AsNoTracking();
        #region 進行搜尋動作
        if (!string.IsNullOrWhiteSpace(dataRequest.Search))
        {
            DataSource = DataSource
            .Where(x => x.Account.Contains(dataRequest.Search) ||
                        x.Name.Contains(dataRequest.Search));
        }
        #endregion

        #region 進行排序動作
        if (!string.IsNullOrWhiteSpace(dataRequest.SortField))
        {
            if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.Account), StringComparison.OrdinalIgnoreCase))
            {
                if (dataRequest.SortDescending)
                {
                    DataSource = DataSource
                        .OrderByDescending(x => x.Account)
                        .ThenByDescending(x => x.Id);
                }
                else
                {
                    DataSource = DataSource
                        .OrderBy(x => x.Account)
                        .ThenBy(x => x.Id);
                }
            }
            else if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.Name), StringComparison.OrdinalIgnoreCase))
            {
                if (dataRequest.SortDescending)
                {
                    DataSource = DataSource
                        .OrderByDescending(x => x.Name)
                        .ThenByDescending(x => x.Id);
                }
                else
                {
                    DataSource = DataSource
                        .OrderBy(x => x.Name)
                        .ThenBy(x => x.Id);
                }
            }
            else if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.Email), StringComparison.OrdinalIgnoreCase))
            {
                if (dataRequest.SortDescending)
                {
                    DataSource = DataSource
                        .OrderByDescending(x => x.Email)
                        .ThenByDescending(x => x.Id);
                }
                else
                {
                    DataSource = DataSource
                        .OrderBy(x => x.Email)
                        .ThenBy(x => x.Id);
                }
            }
            else if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.Status), StringComparison.OrdinalIgnoreCase))
            {
                if (dataRequest.SortDescending)
                {
                    DataSource = DataSource
                        .OrderByDescending(x => x.Status)
                        .ThenByDescending(x => x.Id);
                }
                else
                {
                    DataSource = DataSource
                        .OrderBy(x => x.Status)
                        .ThenBy(x => x.Id);
                }
            }
            else if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.IsAdmin), StringComparison.OrdinalIgnoreCase))
            {
                if (dataRequest.SortDescending)
                {
                    DataSource = DataSource
                        .OrderByDescending(x => x.IsAdmin)
                        .ThenByDescending(x => x.Id);
                }
                else
                {
                    DataSource = DataSource
                        .OrderBy(x => x.IsAdmin)
                        .ThenBy(x => x.Id);
                }
            }
            else if (dataRequest.SortField.Equals(nameof(MyUserAdapterModel.Id), StringComparison.OrdinalIgnoreCase))
            {
                DataSource = dataRequest.SortDescending
                    ? DataSource.OrderByDescending(x => x.Id)
                    : DataSource.OrderBy(x => x.Id);
            }
        }
        #endregion

        #region 進行分頁
        // 取得記錄總數量，將要用於分頁元件面板使用
        result.Count = DataSource.Cast<MyUser>().Count();
        DataSource = DataSource.Skip((dataRequest.CurrentPage - 1) * dataRequest.PageSize);
        if (dataRequest.Take != 0)
        {
            DataSource = DataSource.Take(dataRequest.PageSize);
        }
        #endregion

        #region 在這裡進行取得資料
        List<MyUserAdapterModel> adapterModelObjects =
            Mapper.Map<List<MyUserAdapterModel>>(DataSource);
        #endregion

        result.Result = adapterModelObjects;
        await Task.Yield();
        return result;
    }

    public async Task<MyUserAdapterModel> GetAsync(int id)
    {
        MyUser item = await context.MyUser
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        MyUserAdapterModel result = Mapper.Map<MyUserAdapterModel>(item);
        return result;
    }

    public async Task<VerifyRecordResult> AddAsync(MyUserAdapterModel paraObject)
    {
        try
        {
            CleanTrackingHelper.Clean<MyUser>(context);
            MyUser itemParameter = Mapper.Map<MyUser>(paraObject);

            #region 密碼雜湊處理
            if (!string.IsNullOrWhiteSpace(paraObject.Password))
            {
                string salt = Guid.NewGuid().ToString();
                itemParameter.Salt = salt;
                itemParameter.Password = PasswordHelper.GetPasswordSHA(salt, paraObject.Password);
            }
            #endregion

            await context.MyUser
                .AddAsync(itemParameter);
            await context.SaveChangesAsync();
            CleanTrackingHelper.Clean<MyUser>(context);
            return VerifyRecordResultFactory.Build(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "新增記錄發生例外異常");
            return VerifyRecordResultFactory.Build(false, "新增記錄發生例外異常", ex);
        }
    }

    public async Task<VerifyRecordResult> UpdateAsync(MyUserAdapterModel paraObject)
    {
        try
        {
            CleanTrackingHelper.Clean<MyUser>(context);
            MyUser existingItem = await context.MyUser
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == paraObject.Id);
            if (existingItem == null)
            {
                return VerifyRecordResultFactory.Build(false, "無法修改紀錄");
            }
            else
            {
                MyUser itemData = Mapper.Map<MyUser>(paraObject);

                #region 密碼處理：若有輸入新密碼則重新雜湊；否則保留原有密碼
                if (!string.IsNullOrWhiteSpace(paraObject.Password))
                {
                    string salt = Guid.NewGuid().ToString();
                    itemData.Salt = salt;
                    itemData.Password = PasswordHelper.GetPasswordSHA(salt, paraObject.Password);
                }
                else
                {
                    itemData.Salt = existingItem.Salt;
                    itemData.Password = existingItem.Password;
                }
                #endregion

                CleanTrackingHelper.Clean<MyUser>(context);
                context.Entry(itemData).State = EntityState.Modified;
                await context.SaveChangesAsync();
                CleanTrackingHelper.Clean<MyUser>(context);
                return VerifyRecordResultFactory.Build(true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "修改記錄發生例外異常");
            return VerifyRecordResultFactory.Build(false, "修改記錄發生例外異常", ex);
        }
    }

    public async Task<VerifyRecordResult> DeleteAsync(int id)
    {
        try
        {
            CleanTrackingHelper.Clean<MyUser>(context);
            MyUser item = await context.MyUser
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
            {
                return VerifyRecordResultFactory.Build(false, "無法刪除紀錄");
            }
            else
            {
                CleanTrackingHelper.Clean<MyUser>(context);
                context.Entry(item).State = EntityState.Deleted;
                await context.SaveChangesAsync();
                CleanTrackingHelper.Clean<MyUser>(context);
                return VerifyRecordResultFactory.Build(true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "刪除記錄發生例外異常");
            return VerifyRecordResultFactory.Build(false, "刪除記錄發生例外異常", ex);
        }
    }
    #endregion

    #region CRUD 的限制條件檢查
    public async Task<VerifyRecordResult> BeforeAddCheckAsync(MyUserAdapterModel paraObject)
    {
        var searchItem = await context.MyUser
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Account == paraObject.Account);
        if (searchItem != null)
        {
            return VerifyRecordResultFactory.Build(false, "要新增的紀錄已經存在無法新增");
        }
        return VerifyRecordResultFactory.Build(true);
    }

    public async Task<VerifyRecordResult> BeforeUpdateCheckAsync(MyUserAdapterModel paraObject)
    {
        CleanTrackingHelper.Clean<MyUser>(context);
        var searchItem = await context.MyUser
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == paraObject.Id);
        if (searchItem == null)
        {
            return VerifyRecordResultFactory.Build(false, "要更新的紀錄_發生同時存取衝突_已經不存在資料庫上");
        }

        searchItem = await context.MyUser
           .AsNoTracking()
           .FirstOrDefaultAsync(x => x.Account == paraObject.Account &&
           x.Id != paraObject.Id);
        if (searchItem != null)
        {
            return VerifyRecordResultFactory.Build(false, "要修改的紀錄已經存在無法修改");
        }
        return VerifyRecordResultFactory.Build(true);
    }

    public async Task<VerifyRecordResult> BeforeDeleteCheckAsync(MyUserAdapterModel paraObject)
    {
        try
        {
            return VerifyRecordResultFactory.Build(true);
        }
        catch (Exception ex)
        {
            return VerifyRecordResultFactory.Build(false, "刪除記錄發生例外異常", ex);
        }
    }
    #endregion
}
