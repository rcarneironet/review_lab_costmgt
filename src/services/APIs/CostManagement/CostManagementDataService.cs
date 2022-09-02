﻿using services.Dtos;
using services.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace services.APIs.CostManagement
{
    public class CostManagementDataService
    {
        public async Task<List<WeeklyBillingDto>> GetWeeklyBilling()
        {
            List<WeeklyBillingDto> list = new List<WeeklyBillingDto>();

            try
            {
                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand("select top 8 sum(value) as total, date from [dbo].[billing] group by date order by date", conn);

                using SqlDataReader reader = cmd.ExecuteReader();

                while (await reader.ReadAsync())
                {
                    list.Add(new WeeklyBillingDto()
                    {
                        Total = reader.GetDouble(0),
                        Date = reader.GetDateTime(1),
                    });
                }
                return list;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<BillingDto> GetLatestBillingForToday(string subscriptionId)
        {

            if (string.IsNullOrEmpty(subscriptionId))
                return null;

            try
            {
                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand("select date, subscriptionId, value from billing where subscriptionId = @subId and date = @today", conn);
                cmd.Parameters.Add("@subId", SqlDbType.VarChar).Value = subscriptionId;
                cmd.Parameters.Add("@today", SqlDbType.Date).Value = DateTime.Now.Date;

                using SqlDataReader reader = cmd.ExecuteReader();

                var data = new BillingDto();

                while (await reader.ReadAsync())
                {
                    data.Date = reader.GetDateTime(0);
                    data.SubscriptionId = reader.GetString(1);
                    data.Value = reader.GetDouble(2);
                }
                return data;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<List<MonthToDateDto>> GetMonthToDateBilling()
        {
            List<MonthToDateDto> list = new List<MonthToDateDto>();

            try
            {
                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand("select subscriptionId, Value, DATENAME(month, getdate()) as Month from [dbo].[billing] where date = @today", conn);
                cmd.Parameters.Add("@today", SqlDbType.Date).Value = DateTime.Now.Date;

                using SqlDataReader reader = cmd.ExecuteReader();

                while (await reader.ReadAsync())
                {
                    list.Add(new MonthToDateDto()
                    {
                        SubscriptionId = reader.GetString(0),
                        Value = reader.GetDouble(1),
                        Month = reader.GetString(2)
                    });
                }
                return list;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task SaveBilling(BillingDto dto)
        {
            try
            {

                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                await conn.OpenAsync();

                var sql = string.Empty;
                if (!dto.IsUpdate)
                    sql = "insert into [dbo].[billing] values(@date, @subscriptionId, @value, getdate());";
                else
                    sql = "update [dbo].[billing] set value = @value, lastupdated = getdate() where date = @date and subscriptionId = @subscriptionId;";


                if (dto.PercentChanged >= Utils.PercentageWarning)
                {
                    sql += " insert into [dbo].[billinglog] values (NEWID(), getdate(), @subscriptionId, @value, @valuechangepercent, 0);";
                }

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@date", SqlDbType.Date).Value = dto.Date;
                cmd.Parameters.Add("@subscriptionId", SqlDbType.VarChar, 50).Value = dto.SubscriptionId;
                cmd.Parameters.Add("@value", SqlDbType.Float).Value = dto.Value;
                cmd.Parameters.Add("@valuechangepercent", SqlDbType.Float).Value = dto.PercentChanged;

                cmd.CommandType = CommandType.Text;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public List<BillingLogDto> NotifyConsumptionIncreaseByEmail()
        {
            List<BillingLogDto> list = new List<BillingLogDto>();

            try
            {
                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                conn.Open();

                SqlCommand cmd = new SqlCommand("select id, date, subscriptionId, value, valuechangepercent, emailsent from [dbo].[billinglog] where emailsent = 0", conn);

                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new BillingLogDto()
                    {
                        Id = reader.GetGuid(0),
                        Date = reader.GetDateTime(1),
                        SubscriptionId = reader.GetString(2),
                        Value = reader.GetDouble(3),
                        ValueChangePercent = reader.GetDouble(4),
                        IsEmailSent = reader.GetBoolean(5)
                    });
                }
                return list;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public void UpdateEmailNotification(Guid id)
        {
            try
            {

                using SqlConnection conn = new SqlConnection(Utils.DbConnectionString);
                conn.OpenAsync();

                var sql = "update [dbo].[billinglog] set emailsent = 1 where id = @id;";

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;

                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}