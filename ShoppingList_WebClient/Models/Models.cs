using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Models
{
    public class Log
    {
        public long LogId { get; set; }
        public string LogLevel { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string CreatedDate { get; set; }
        public long? UserId { get; set; }
        public Log Inner { get; set; }

    }

    public class MessageSatus
    {
        public const string OK = "OK";
        public const string Error = "ERROR";

    }
    public class MessageAndStatus
    {
        public bool IsError => !IsSuccess;
        public bool IsSuccess => Status == MessageSatus.OK;
        public string Status { get; set; }
        public string Message { get; set; }

        protected MessageAndStatus(string message, string status)
        {
            Status = status;
            Message = message;
        }
        public MessageAndStatus()
        {

        }

        public static MessageAndStatus Ok(string msg) => new MessageAndStatus(msg, MessageSatus.OK);
        public static MessageAndStatus Fail(string msg) => new MessageAndStatus(msg, MessageSatus.Error);

    }
    public class MessageAndStatusAndData<T> : MessageAndStatus
    {
        public MessageAndStatusAndData(T data, string msg, string status)
        {
            Data = data;
            Message = msg;
            Status = status;
        }

        public T Data { get; set; }

        public static MessageAndStatusAndData<T> Ok(T data) =>
            new MessageAndStatusAndData<T>(data, string.Empty, MessageSatus.OK);

        public static MessageAndStatusAndData<T> Fail(string msg) =>
           new MessageAndStatusAndData<T>(default, msg, MessageSatus.Error);
    }
    public static class ItemState
    {
        public static int Normal => 0;
        public static int Buyed => 1;

    }

    public class RegistrationModel
    {

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string UserName { get; set; }

        [MinLength(6, ErrorMessage = "Minimal lenght is 6")]
        [MaxLength(50)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Passwords are not equall.")]
        [DataType(DataType.Password)]
        [Required]
        public string PasswordConfirm { get; set; }

    }


    public class Invitation : IModelItemView
    {
        public int InvitationId { get; set; }
        public string EmailAddress { get; set; }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public string SenderName { get; set; }

        public int Id => InvitationId;

        public string Name
        {
            get { return EmailAddress; }
            set { EmailAddress = value; }
        }

    }
    public class ListAggregationWithUsersPermission
    {

        public ListAggregator ListAggregator { get; set; }

        public List<UserPermissionToListAggregation> UsersPermToListAggr { get; set; }
    }

    public class UserPermissionToListAggregation : IModelItemView
    {
        public User User { get; set; }
        public int Permission { get; set; }


        public int Order { get; set; }

        public int Id => User.UserId;
        public string Name
        {
            get { return User.EmailAddress; }
            set { User.EmailAddress = value; }
        }

    }

    public class User
    {


        public User()
        {
            ListAggregators = new List<ListAggregator>();


        }

        public int UserId { get; set; }
        public string EmailAddress { get; set; }
        // public string Password { get; set; }

        public byte LoginType { get; set; } // 1 - local 2 - facebook ()
        public IList<ListAggregator> ListAggregators { get; set; }


    }




    public class ListAggregator : IModelItem
    {

        public ListAggregator()
        {
            Lists = new List<List>();

        }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public int Order { get; set; }
        public string Name
        {
            get { return ListAggregatorName; }
            set { ListAggregatorName = value; }
        }

        public int Id => ListAggregatorId;

        public IList<List> Lists { get; set; }

    }



    public class OrderListItem : IModelItemOrder
    {

        public OrderListItem()
        {
            List = new List<OrderItem>();
        }

        public List<OrderItem> List { get; set; }
        public int Id { get; set; }

        public int Order { get; set; }
    }

    public class OrderListAggrItem : IModelItemOrder
    {

        public OrderListAggrItem()
        {
            List = new List<OrderListItem>();
        }

        public List<OrderListItem> List { get; set; }
        public int Id { get; set; }

        public int Order { get; set; }
    }

    public class OrderItem : IModelItemOrder
    {
        public int Id { get; set; }

        public int Order { get; set; }

    }


    public class List : IModelItem
    {
        public List()
        {
            ListItems = new List<ListItem>();

        }
        public int ListId { get; set; }
        public string ListName { get; set; }
        public int Order { get; set; }
        public int Id => ListId;
        public string Name
        {
            get { return ListName; }
            set { ListName = value; }
        }
        public IList<ListItem> ListItems { get; set; }

    }


    public interface IModelItem : IModelItemView, IModelItemOrder
    {


    }

    public interface IModelItemView : IModelItemBase
    {

        public string Name { get; set; }

        //public int Id { get; set; }

    }

    public interface IModelItemOrder : IModelItemBase
    {

        public int Order { get; set; }

        //public int Id { get; set; }

    }

    public interface IModelItemBase
    {

        public int Id { get; }
    }


    public class ListItem : IModelItem
    {
        public int ListItemId { get; set; }

        public int Order { get; set; }
        public int State { get; set; }


        [Required]
        [MinLength(2, ErrorMessage = "Minimalna długośc to 2")]
        public string ListItemName { get; set; }

        public int Id => ListItemId;
        public string Name
        {
            get { return ListItemName; }
            set { ListItemName = value; }
        }

    }

    public static class SiganalREventName
    {
        public const string ListItemEdited = nameof(ListItemEdited);
        public const string ListItemAdded = nameof(ListItemAdded);
        public const string ListItemDeleted = nameof(ListItemDeleted);
        public const string InvitationAreChanged = nameof(InvitationAreChanged);
        public const string DataAreChanged = nameof(DataAreChanged);
    }

    public record SignaREnvelope(string SignalRId, string SiglREventName, string SerializedEvent);
    public abstract record ListItemSignalREvent(int ListItemId, int ListAggregationId);
    public record ListItemAddedSignalREvent(int ListItemId, int ListAggregationId, int ListId) : ListItemSignalREvent(ListItemId, ListAggregationId);
    public record ListItemDeletedSignalREvent(int ListItemId, int ListAggregationId) : ListItemSignalREvent(ListItemId, ListAggregationId);
    public record ListItemEditedSignalREvent(int ListItemId, int ListAggregationId) : ListItemSignalREvent(ListItemId, ListAggregationId);
}
