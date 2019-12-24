<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Prog4WebDatabase._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

   

    <div class="row">
            <h2>Cloud Database and Storage</h2>
            <p>
                Use the Load Data button to bring in data from predefined url.</p>
            <p>
                Use the Clear Data button to remove all entries from both storage locations.</p>
            <p>
                Use the Query button to search the database. Searches require exact spelling.</p>
            <p>
                First Name&nbsp;
                <asp:TextBox ID="TextBox_firstname" runat="server"></asp:TextBox>
            </p>
            <p>
                Last Name&nbsp;
                <asp:TextBox ID="TextBox_lastname" runat="server"></asp:TextBox>
            </p>
            <p>
                <asp:Button ID="LoadDataBtn" runat="server" OnClick="LoadDataBtn_Click" Text="Load Data" />
&nbsp;&nbsp;&nbsp;
                <asp:Button ID="ClearDataBtn" runat="server" OnClick="ClearDataBtn_Click" Text="Clear Data" />
&nbsp;&nbsp;&nbsp;
                <asp:Button ID="QueryBtn" OnClick="QueryBtn_Click" runat="server" Text="Query" />
            </p>
    </div>
     <div class="jumbotron">
        <h2>
            <asp:Label ID = "ResultTitle" runat="server" Text=" "></asp:Label>
        </h2>
        <p> 
            
            <asp:Label ID="Results1" runat="server" Text=" "></asp:Label>
            
        </p>
         <h3>
            <asp:Label ID="Error1" runat="server" Text=" "></asp:Label>
         </h3>
    </div>

</asp:Content>
