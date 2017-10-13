using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SoftBrands.FourthShift.Transaction;
using System.IO;
using System.Data;

namespace FS4Amalgamma
{
    public class AmalgammaFSTI
    {
        #region Private Variables
        private FSTIClient _fstiClient = null;
        private string CFGFile = "";
        private string FSUser = "";
        private string FSPass = "";
        private ITransaction _Trans = null;
        private FSTIError itemError = null;

        //Amalgama Produccion
        Data_Base_MNG.SQL DBMNG = new Data_Base_MNG.SQL("192.168.0.11", "AmalgammaDB", "amalgamma", "capsonic");//el paso
        Data_Base_MNG.SQL DBMNG_FS = new Data_Base_MNG.SQL("192.168.0.9", "FSDBMR", "AmalAdmin", "Amalgamma16");//el paso

        //Amalgamma Sand Box
        //Data_Base_MNG.SQL DBMNG = new Data_Base_MNG.SQL("RSSERVER", "AmalgammaDB", "sa", "6rzq4d1");//el paso
        //Data_Base_MNG.SQL DBMNG_FS = new Data_Base_MNG.SQL("CAPTEST", "FSDBMR", "sa", "6rzq4d1");//el paso

        //private string ErrorDBinUse = "Data Temporarily In Use ... Please Try Again";
        string ErrorDBinUse = "Data Temporarily In Use ... Please Try Again";
        string ErrorDBinUse2 = "Manufacturing Data Base In Use ... Please Try Again";
        #endregion

        #region Public Variables
        public bool DBinUseFlag = false;
        public string FSTI_ErrorMsg = "";
        public string Trans_Error_Msg = "";
        public string CDFResponse = "";
        public List<string> DetailError = new List<string>();
        #endregion

        #region FSTI Behaving flags
        private bool FSTI_Is_Initialized = false;
        private bool FSTI_Is_Loged = false;
        #endregion

        #region Private Functions
        private string FSTI_Log(string CFGResponse, string User, string Type, string Error)
        {
            Data_Base_MNG.SQL DBMNG = new Data_Base_MNG.SQL("192.168.0.11", "AmalgammaDB", "amalgamma", "capsonic");

            Error = Error.Replace("'", "");

            string query = "INSERT INTO Amal_FSTI_Log (Amal_User ,Amal_Trans_Type ,Amal_Trans_Response ,Time_Stamp ,Amal_CFGResponse) " +
                            "VALUES ('" + User + "','" + Type + "','" + Error + "',Getdate(),'" + CFGResponse + "')";

            DBMNG.Execute_Command(query);

            return DBMNG.Error_Mjs;
        }
        private void FSTI_Initialization()
        {
            FSTI_ErrorMsg = "";
            try
            {
                _fstiClient = new FSTIClient();

                // call InitializeByConfigFile
                // second parameter == true is to participate in unified logon
                // third parameter == false, no support for impersonation is needed

                _fstiClient.InitializeByConfigFile(CFGFile, true, false);

                // Since this program is participating in unified logon, need to
                // check if a logon is required.

                if (_fstiClient.IsLogonRequired)
                {
                    // Logon is required, enable the logon button
                    //FSTI_Login.Enabled = true;
                    //FSTI_Login.Focus();
                    FSTI_Is_Initialized = true;
                    FSTI_Is_Loged = false;
                }
                else
                {
                    // Logon is not required (because of unified logon), enable the SubmitItem button

                    FSTI_Is_Initialized = true;
                    FSTI_Is_Loged = true;
                }
                // Disable the Initialize button


            }
            catch (FSTIApplicationException exception)
            {
                FSTI_ErrorMsg = exception.Message;
                FSTI_Is_Initialized = false;
                FSTI_Is_Loged = false;
                //MessageBox.Show(exception.Message, "FSTIApplication Exception");
                _fstiClient.Terminate();
                _fstiClient = null;
            }
            //return FSTI_Is_Initialized;
        }
        private void FSTI_Login()
        {
            string message = null;     // used to hold a return message, from the logon

            int status;         // receives the return value from the logon call

            if (!FSTI_Is_Loged)
            {

                status = _fstiClient.Logon(FSUser, FSPass, ref message);
                if (status > 0)
                {
                    FSTI_ErrorMsg = "Invalid user id or password";
                    FSTI_Is_Loged = false;
                }
                else
                {
                    FSTI_Is_Loged = true;
                }
            }

        }
        private void FSTI_STOP()
        {
            if (_fstiClient != null)
            {
                try
                {
                    _fstiClient.Terminate();
                    _fstiClient = null;
                    FSTI_Is_Initialized = false;
                    FSTI_Is_Loged = false;
                }
                catch
                {
                    _fstiClient = null;
                    FSTI_Is_Initialized = false;
                    FSTI_Is_Loged = false; 
                }
            }
        }
        private void FSTI_Execute()
        {
            _fstiClient.ProcessId(_Trans, null);
        }
        private bool FSTI_ProcessTransaction(ITransaction Transaction, string TransactionID, string Ammalgamma_User)
        {
            CDFResponse = Transaction.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(Transaction, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, TransactionID, "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                // DumpErrorObject(myItem, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, TransactionID, Trans_Error_Msg);
                return false;
            }
        }
        private void DumpErrorObject(ITransaction transaction, FSTIError fstiErrorObject)
        {
            DetailError.Clear();

            if (fstiErrorObject.Description.Contains(ErrorDBinUse) || fstiErrorObject.Description.Contains(ErrorDBinUse2))
            {
                DBinUseFlag = true;
            }
            else
            {
                DBinUseFlag = false;
            }

            try
            {
                DetailError.Add("Transaction Error:");
                DetailError.Add("");
                DetailError.Add("Transaction: " + transaction.Name);
                DetailError.Add("Description: " + fstiErrorObject.Description);
                DetailError.Add("MessageFound: " + fstiErrorObject.MessageFound);
                DetailError.Add("MessageID: " + fstiErrorObject.MessageID);
                DetailError.Add("MessageSource: " + fstiErrorObject.MessageSource);
                DetailError.Add("Number: " + fstiErrorObject.Number);
                DetailError.Add("Fields in Error: " + fstiErrorObject.NumberOfFieldsInError);
                for (int i = 0; i < fstiErrorObject.NumberOfFieldsInError; i++)
                {
                    int field = fstiErrorObject.GetFieldNumber(i);
                    DetailError.Add("Field[" + i.ToString() + "]: " + field);
                    ITransactionField myField = transaction.get_Field(field);
                    DetailError.Add("Field name: " + myField.Name);
                }
            }
            catch (Exception ex)
            {
                DetailError.Clear();
                DetailError.Add("Transaction Error description: " + _fstiClient.TransactionError);
                DetailError.Add("Detailed Error Exeption: " + ex.Message);
            }
        }

        #endregion

        #region Constructors

        public AmalgammaFSTI(string CFG_File, string FS_User, string FS_Pass)
        {
            CFGFile = CFG_File;
            FSUser = FS_User;
            FSPass = FS_Pass;
        }

        #endregion

        #region Public Function
        public bool AmalgammaFSTI_Initialization()
        {
            if (!FSTI_Is_Initialized)
            {
                FSTI_Initialization();
            }
            return FSTI_Is_Initialized;
        }
        public void AmalgammaFSTI_Stop()
        {
            FSTI_STOP();
        }
        public bool AmalgammaFSTI_Logon()
        {
            if (!FSTI_Is_Loged)
            {
                FSTI_Login();
            }
            return FSTI_Is_Loged;
        }
        public string[] DumpError(ITransaction transaction, FSTIError fstiErrorObject)
        {
            List<string> errorLog = new List<string>();
            errorLog.Add("Transaction Error:");
            errorLog.Add("");
            errorLog.Add(String.Format("Transaction: {0}", transaction.Name));
            errorLog.Add(String.Format("Description: {0}", fstiErrorObject.Description));
            errorLog.Add(String.Format("MessageFound: {0} ", fstiErrorObject.MessageFound));
            errorLog.Add(String.Format("MessageID: {0} ", fstiErrorObject.MessageID));
            errorLog.Add(String.Format("MessageSource: {0} ", fstiErrorObject.MessageSource));
            errorLog.Add(String.Format("Number: {0} ", fstiErrorObject.Number));
            errorLog.Add(String.Format("Fields in Error: {0} ", fstiErrorObject.NumberOfFieldsInError));
            for (int i = 0; i < fstiErrorObject.NumberOfFieldsInError; i++)
            {
                int field = fstiErrorObject.GetFieldNumber(i);
                errorLog.Add(String.Format("Field[{0}]: {1}", i, field));
                ITransactionField myField = transaction.get_Field(field);
                errorLog.Add(String.Format("Field name: {0}", myField.Name));
            }
            return errorLog.ToArray();
        }

        #endregion

        #region FSTI Transactions
        #region Standard Transactions
        public bool AmalgammaFSTI_MORV00(string FSTI_fields, string Ammalgamma_User)
        {
            bool LOT20CHAR_FLAG = false;
            MORV00 myMORV00 = new MORV00();
            string[] Fields_Array = FSTI_fields.Split(',');

            #region Fields
            //fields= MO_NO,LN_NO,RECV_QTY,ITEM,STK,BIN,RECV_TYPE,REMARKS,LOT
            //MS-051016-141,001,64,934409,FL,24-00,R,1JUN9572087701616969,1JUN9572087701616969

            string MO_LN_TYPE_Check_Q = "SELECT FS_MOLine.MOLineType FROM FS_MOLine INNER JOIN " +
                            "FS_MOHeader ON FS_MOLine.MOHeaderKey = FS_MOHeader.MOHeaderKey " +
                            "WHERE (FS_MOHeader.MONumber = '" + Fields_Array[0] + "') AND (FS_MOLine.MOLineNumber = '" +
                            Fields_Array[1] + "')";
            string MO_Balance_Check_Q = "SELECT (FS_MOLine.ItemOrderedQuantity- FS_MOLine.ReceiptQuantity) as Balance FROM FS_MOLine INNER JOIN " +
                            "FS_MOHeader ON FS_MOLine.MOHeaderKey = FS_MOHeader.MOHeaderKey " +
                            "WHERE (FS_MOHeader.MONumber = '" + Fields_Array[0] + "') AND (FS_MOLine.MOLineNumber = '" +
                            Fields_Array[1] + "')";

            string MO_NO = Fields_Array[0];//
            string LN_NO = Fields_Array[1];//
            string RECV_TYPE = "R";//
            string RECV_QTY = Fields_Array[2];//
            string LN_TYPE = DBMNG_FS.Execute_Scalar(MO_LN_TYPE_Check_Q);//
            string ITEM = Fields_Array[3];//

            string MOVE_QTY = RECV_QTY;
            string STK = Fields_Array[4].Trim();
            string BIN = Fields_Array[5].Trim();
            string INV_CAT = "O";
            string INSP_CODE = "G";

            string LotCheck = "SELECT IsLotTraced FROM  FS_Item WHERE (ItemNumber = '" + ITEM + "')";
            string NEW_LOT = "Y";
            string LOT_ASSIGN_POL = "";
            string REMARK = "";


            try
            {
                RECV_TYPE = Fields_Array[6].Trim();
            }
            catch
            {
                RECV_TYPE = "R";
            }

            try
            {
                REMARK = Fields_Array[7].Trim();
            }
            catch
            {
                REMARK = "";
            }
            try
            {
                NEW_LOT = Fields_Array[8].Trim();
                if (NEW_LOT.Length > 20)
                {
                    LOT20CHAR_FLAG = true;
                    //NEW_LOT = NEW_LOT.Replace("UN", "");
                }
            }
            catch
            {
                NEW_LOT = "";
            }


            //MO Number
            myMORV00.MONumber.Value = MO_NO;
            //MO Line
            myMORV00.MOLineNumber.Value = LN_NO;
            //MO Line Type
            myMORV00.MOLineType.Value = LN_TYPE;
            //Reciving Type
            myMORV00.ReceivingType.Value = RECV_TYPE;
            //Reciving QTY
            myMORV00.ReceiptQuantity.Value = RECV_QTY;
            //Item Number
            myMORV00.ItemNumber.Value = ITEM;

            //Move QTY
            myMORV00.MoveQuantity1.Value = RECV_QTY;
            //Stock
            myMORV00.Stockroom1.Value = STK;
            //Bin
            myMORV00.Bin1.Value = BIN;
            //Inventory Category
            myMORV00.InventoryCategory1.Value = INV_CAT;
            //Inspection Code
            myMORV00.InspectionCode1.Value = INSP_CODE;
            //Remarks
            myMORV00.Remark.Value = REMARK;

            //lot tracing
            #region LotTrace

            if (("Y" == DBMNG_FS.Execute_Scalar(LotCheck)) && (!LOT20CHAR_FLAG))
            {

                myMORV00.LotNumber.Value = NEW_LOT;
                myMORV00.LotNumberDefault.Value = NEW_LOT;
                myMORV00.LotDescription.Value = NEW_LOT;
                myMORV00.IsNewLot.Value = "Y";
                myMORV00.LotNumberAssignmentPolicy.Value = "C";
                //myMORV00.TextID.Value = "032415";
            }

            #endregion
            //

            //myMORV00.
            #endregion

            CDFResponse = myMORV00.GetString(TransactionStringFormat.fsCDF);

            DBinUseFlag = false;
            if (_fstiClient.ProcessId(myMORV00, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "MORV00", "");
                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                DumpErrorObject(myMORV00, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "MORV00", Trans_Error_Msg);
                return false;
            }

        }
        public bool AmalgammaFSTI_ITMB03(string FSTI_fields, string Ammalgamma_User)
        {
            ITMB03 MyITMB03 = new ITMB03();

            //IntemNumber,Planer,Buyer
            string[] Fields_Array = FSTI_fields.Split(',');

            //Item Number
            string ItemNo = Fields_Array[0];
            //Item Planer
            string NewPlaner = Fields_Array[1];
            //Item Buyer
            string NewBuyer = Fields_Array[1];

            MyITMB03.ItemNumber.Value = ItemNo;
            MyITMB03.Planner.Value = NewPlaner;
            MyITMB03.Buyer.Value = NewBuyer;

            //myMORV00.

            CDFResponse = MyITMB03.GetString(TransactionStringFormat.fsCDF);

            DBinUseFlag = false;
            if (_fstiClient.ProcessId(MyITMB03, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "ITMB03", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                DumpErrorObject(MyITMB03, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "ITMB03", Trans_Error_Msg);
                return false;
            }

        }
        public bool AmalgammaFSTI_IMTR01(string FSTI_fields, string Ammalgamma_User)
        {
            //Transaction Object
            IMTR01 MyIMTR01 = new IMTR01();

            try
            {
                //IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty,lot,lot_to,Newlot
                //          0,          1,         2,        3,       4,  5,  6,     7,     8
                string[] Fields_Array = FSTI_fields.Split(',');

                //Item Number
                MyIMTR01.ItemNumber.Value = Fields_Array[0];

                //from

                string[] LocFrom = Fields_Array[1].Split('-');
                string BinFrom = "";

                if (LocFrom.Count() > 2)
                {
                    for (int i = 1; i < LocFrom.Count(); i++)
                    {
                        if (i == 1)
                        {
                            BinFrom += LocFrom[i];
                        }
                        else
                        {
                            BinFrom += "-" + LocFrom[i];
                        }
                    }
                }
                else
                {
                    BinFrom = LocFrom[1];
                }

                MyIMTR01.StockroomFrom.Value = LocFrom[0];
                MyIMTR01.BinFrom.Value = BinFrom.Replace(" ", "");

                MyIMTR01.InventoryCategoryFrom.Value = Fields_Array[2];

                //to

                //IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty

                string[] LocTo = Fields_Array[3].Split('-');
                string BinTo = "";

                if (LocTo.Count() > 2)
                {
                    for (int i = 1; i < LocTo.Count(); i++)
                    {
                        if (i == 1)
                        {
                            BinTo += LocTo[i];
                        }
                        else
                        {
                            BinTo += "-" + LocTo[i];
                        }
                    }
                }
                else
                {
                    BinTo = LocTo[1];
                }


                MyIMTR01.StockroomTo.Value = LocTo[0];
                MyIMTR01.BinTo.Value = BinTo.Replace(" ", "");

                MyIMTR01.InventoryCategoryTo.Value = Fields_Array[4];

                //QTY
                MyIMTR01.InventoryQuantity.Value = Fields_Array[5];

                //Lot Tracing
                #region LotTracing
                string LotCheck = "SELECT IsLotTraced FROM  FS_Item WHERE (ItemNumber = '" + Fields_Array[0] + "')";
                string NewLotExist_Q = "";
                try
                {
                    NewLotExist_Q = "SELECT COUNT(LotNumber) AS EXIST FROM FS_LotTrace WHERE (LotNumber = '" + Fields_Array[7] + "')";
                }
                catch { }
                string NewLotExist = DBMNG_FS.Execute_Scalar(NewLotExist_Q);


                string NEW_LOT = "";

                try
                {
                    NEW_LOT = Fields_Array[6].Trim();
                    if (NEW_LOT.Length > 20)
                    {
                        NEW_LOT = NEW_LOT.Replace("UN", "");
                    }
                }
                catch
                {
                    NEW_LOT = "";
                }

                if ("Y" == DBMNG_FS.Execute_Scalar(LotCheck))
                {
                    MyIMTR01.LotIdentifier.Value = "E";
                    MyIMTR01.LotNumberFrom.Value = NEW_LOT;

                    try
                    {
                        if (Fields_Array[7].Trim() != "")
                        {
                            MyIMTR01.LotNumberTo.Value = Fields_Array[7].Trim();// lot to
                        }
                        else
                        {
                            MyIMTR01.LotNumberTo.Value = NEW_LOT;
                        }
                    }
                    catch
                    {
                        MyIMTR01.LotNumberTo.Value = NEW_LOT;
                    }

                }
                else
                {
                    MyIMTR01.LotIdentifier.Value = "N";
                }

                try
                {
                    if (NewLotExist == "0")
                    {
                        string MFGDATE_Q = "SELECT FirstReceiptDate FROM FS_LotTrace WHERE (LotNumber = '" + Fields_Array[7] + "')";
                        MyIMTR01.LotIdentifier.Value = "Y";
                        MyIMTR01.ItemLotReceiptWindow.Value = "Y";
                        MyIMTR01.LotDescription.Value = Fields_Array[7];
                        MyIMTR01.LotNumberDefault.Value = Fields_Array[7];
                        MyIMTR01.LotNumberAssignmentPolicy.Value = "C";
                        //MyIMTR01.FirstReceiptDate.Value = DateTime.Now.ToString("MMddyy");
                        MyIMTR01.FirstReceiptDate.Value = DateTime.Parse(DBMNG_FS.Execute_Scalar(MFGDATE_Q)).ToString("MMddyy");

                    }
                    else
                    {
                        MyIMTR01.LotIdentifier.Value = "E";
                        MyIMTR01.LotNumberAssignmentPolicy.Value = "E";
                    }
                }
                catch { }

                #endregion
                ///

                CDFResponse = MyIMTR01.GetString(TransactionStringFormat.fsCDF);

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyIMTR01, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "ITMR01", "");

                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyIMTR01, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "ITMR01", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "ITMR01", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_IMTR01_RELABEL(string FSTI_fields, string Ammalgamma_User)
        {
            //Transaction Object
            IMTR01 MyIMTR01 = new IMTR01();

            try
            {
                //IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty,lot,lot_to
                string[] Fields_Array = FSTI_fields.Split(',');

                //Item Number
                MyIMTR01.ItemNumber.Value = Fields_Array[0];

                //from

                string[] LocFrom = Fields_Array[1].Split('-');
                string BinFrom = "";

                if (LocFrom.Count() > 2)
                {
                    for (int i = 1; i < LocFrom.Count(); i++)
                    {
                        if (i == 1)
                        {
                            BinFrom += LocFrom[i];
                        }
                        else
                        {
                            BinFrom += "-" + LocFrom[i];
                        }
                    }
                }
                else
                {
                    BinFrom = LocFrom[1];
                }

                MyIMTR01.StockroomFrom.Value = LocFrom[0];
                MyIMTR01.BinFrom.Value = BinFrom.Replace(" ", "");

                MyIMTR01.InventoryCategoryFrom.Value = Fields_Array[2];

                //to

                //IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty

                string[] LocTo = Fields_Array[3].Split('-');
                string BinTo = "";

                if (LocTo.Count() > 2)
                {
                    for (int i = 1; i < LocTo.Count(); i++)
                    {
                        if (i == 1)
                        {
                            BinTo += LocTo[i];
                        }
                        else
                        {
                            BinTo += "-" + LocTo[i];
                        }
                    }
                }
                else
                {
                    BinTo = LocTo[1];
                }


                MyIMTR01.StockroomTo.Value = LocTo[0];
                MyIMTR01.BinTo.Value = BinTo.Replace(" ", "");

                MyIMTR01.InventoryCategoryTo.Value = Fields_Array[4];

                //QTY
                MyIMTR01.InventoryQuantity.Value = Fields_Array[5];

                //Lot Tracing
                
                #region LotTracing
                //string LotCheck = "SELECT IsLotTraced FROM  FS_Item WHERE (ItemNumber = '" + Fields_Array[0] + "')";

                //string NEW_LOT = "";

                //try
                //{
                //    NEW_LOT = Fields_Array[6].Trim();
                //    if (NEW_LOT.Length > 20)
                //    {
                //        NEW_LOT = NEW_LOT.Replace("UN", "");
                //    }
                //}
                //catch
                //{
                //    NEW_LOT = "";
                //}

                //if ("Y" == DBMNG_FS.Execute_Scalar(LotCheck))
                //{
                //    MyIMTR01.LotIdentifier.Value = "E";
                //    MyIMTR01.LotNumberFrom.Value = NEW_LOT;

                //    try
                //    {
                //        if (Fields_Array[7].Trim() != "")
                //        {
                //            MyIMTR01.LotNumberTo.Value = Fields_Array[7].Trim();// lot to
                //        }
                //        else
                //        {
                //            MyIMTR01.LotNumberTo.Value = NEW_LOT;
                //        }
                //    }
                //    catch
                //    {
                //        MyIMTR01.LotNumberTo.Value = NEW_LOT;
                //    }

                //}
                //else
                //{
                //    MyIMTR01.LotIdentifier.Value = "N";
                //}

                #endregion

                //IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty,lot,lot_to
                //          0,          1,         3,        4,       5,  6,  7,     8
                MyIMTR01.LotNumberTo.Value = Fields_Array[8];
                MyIMTR01.LotIdentifier.Value = "C";
                MyIMTR01.LotNumberAssignmentPolicy.Value = "C";
                MyIMTR01.ItemLotReceiptWindow.Value = "Y";
                MyIMTR01.LotDescription.Value = Fields_Array[0];
                MyIMTR01.FirstReceiptDate.Value = DateTime.Now.ToString("MMddyy");

                ///

                CDFResponse = MyIMTR01.GetString(TransactionStringFormat.fsCDF);

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyIMTR01, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "ITMR01", "");

                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyIMTR01, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "IMTR01", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "IMTR01", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_IMTR01_NEWLOT(string FSTI_fields, string Ammalgamma_User)
        {
            //Transaction Object
            IMTR01 MyIMTR01 = new IMTR01();

            try
            {
                //IntemNumber,STK-BIN,InvCat,Qty,lot, MGF_DATE
                string[] Fields_Array = FSTI_fields.Split(',');

                //Item Number
                MyIMTR01.ItemNumber.Value = Fields_Array[0];

                //from

                string[] LocFrom = Fields_Array[1].Split('-');
                string Bin = "";

                if (LocFrom.Count() > 2)
                {
                    for (int i = 1; i < LocFrom.Count(); i++)
                    {
                        if (i == 1)
                        {
                            Bin += LocFrom[i];
                        }
                        else
                        {
                            Bin += "-" + LocFrom[i];
                        }
                    }
                }
                else
                {
                    Bin = LocFrom[1];
                }

                MyIMTR01.StockroomFrom.Value = LocFrom[0];
                MyIMTR01.BinFrom.Value = Bin;

                MyIMTR01.InventoryCategoryFrom.Value = Fields_Array[2];

                //to

                MyIMTR01.StockroomTo.Value = MyIMTR01.StockroomFrom.Value;
                MyIMTR01.BinTo.Value = MyIMTR01.BinFrom.Value;
                MyIMTR01.InventoryCategoryTo.Value = MyIMTR01.InventoryCategoryFrom.Value;



                //QTY
                MyIMTR01.InventoryQuantity.Value = Fields_Array[3];

                //Lot Tracing
                #region LotTracing
                string LotCheck = "SELECT IsLotTraced FROM  FS_Item WHERE (ItemNumber = '" + Fields_Array[0] + "')";

                string NEW_LOT = "";

                try
                {
                    NEW_LOT = Fields_Array[4].Trim();
                }
                catch
                {
                    NEW_LOT = "";
                }

                if ("Y" == DBMNG_FS.Execute_Scalar(LotCheck))
                {
                    MyIMTR01.LotIdentifier.Value = "C";
                    MyIMTR01.MoveNonLotQuantity.Value = "C";
                    //MyIMTR01.LotNumberFrom.Value = NEW_LOT;
                    MyIMTR01.LotNumberTo.Value = NEW_LOT;
                    MyIMTR01.LotNumberDefault.Value = NEW_LOT;
                    MyIMTR01.FirstReceiptDate.Value = Fields_Array[5];
                    MyIMTR01.ItemLotReceiptWindow.Value = "Y";
                    MyIMTR01.LotNumberAssignmentPolicy.Value = "C";
                }
                else
                {
                    MyIMTR01.LotIdentifier.Value = "N";
                }

                #endregion
                ///

                CDFResponse = MyIMTR01.GetString(TransactionStringFormat.fsCDF);

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyIMTR01, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "IMTR01-NEWLOT", "");

                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyIMTR01, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "ITMR01", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "ITMR01", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_PORV01(string FSTI_fields, string Ammalgamma_User)
        {
            //Transaction Object
            PORV01 MyPORV01 = new PORV01();

            #region Campos
            //PO_Number, Ln#, Receiving_Type, Quantity_Received, Stk, Bin, Item, Promised_Date, Line Tipe, Carrier, Traking , ASN
            //0        , 1  , 2             , 3                , 4  , 5  , 6   , 7            , 8        , 9      , 10      , 11

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPORV01.PONumber.Value = Fields_Array[0];

            //PO_Line#
            MyPORV01.POLineNumber.Value = Fields_Array[1];

            //Receiving_Type
            MyPORV01.POReceiptActionType.Value = Fields_Array[2];

            //QTY RECV
            MyPORV01.ReceiptQuantity.Value = Fields_Array[3];//TOTAL
            MyPORV01.ReceiptQuantityMove1.Value = Fields_Array[3];

            //STK
            MyPORV01.Stockroom1.Value = Fields_Array[4];//TOTAL

            //BIN
            MyPORV01.Bin1.Value = Fields_Array[5];

            //Item Number
            MyPORV01.ItemNumber.Value = Fields_Array[6];

            //Line Tipe
            MyPORV01.POLineType.Value = Fields_Array[8];

            //Lot assign
            MyPORV01.LotNumberAssignmentPolicy.Value = "";

            //Promised Date
            MyPORV01.PromisedDate.Value = Fields_Array[7];

            //Receipt Date
            MyPORV01.POReceiptDate.Value = DateTime.Now.ToString("MMddyy");

            //Carrier Name
            MyPORV01.CarrierName.Value = Fields_Array[9];

            //Traking
            MyPORV01.Remark.Value = Fields_Array[10];

            //ASN
            MyPORV01.PackingSlipNumber.Value = Fields_Array[11];

            //FreightBill  Code
            MyPORV01.FreightBillNumber.Value = Fields_Array[11];

            #endregion

            #region Ejecucion
            CDFResponse = MyPORV01.GetString(TransactionStringFormat.fsCDF);

            DBinUseFlag = false;
            if (_fstiClient.ProcessId(MyPORV01, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "PORV01", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;

                DumpErrorObject(MyPORV01, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "PORV01", Trans_Error_Msg);
                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT00(string FSTI_fields, string Ammalgamma_User)
        {
            POMT00 MyPOMT00 = new POMT00();

            DBinUseFlag = false;
            #region Campos
            //PO_Number, Vendor_ID, Terms
            //0        , 1        , 2    
            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT00.PONumber.Value = Fields_Array[0];

            //Vendor_ID
            MyPOMT00.VendorID.Value = Fields_Array[1];

            //Terms
            MyPOMT00.TermsCode.Value = Fields_Array[2];



            #endregion

            #region Ejecucion
            CDFResponse = MyPOMT00.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT00, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT00", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                DumpErrorObject(MyPOMT00, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT00", Trans_Error_Msg);
                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT10(string FSTI_fields, string Ammalgamma_User)
        {
            POMT10 MyPOMT10 = new POMT10();

            DBinUseFlag = false;
            #region Campos
            //PO_Number, Line_QTY, Line_Status, Line_Type, Item_Num, Prom_Date, Blanket, UM, Unit_price
            //0        , 1       , 2          , 3        , 4       , 5        , 6      , 7 , 8
            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT10.PONumber.Value = Fields_Array[0];

            //Line_QTY
            MyPOMT10.LineItemOrderedQuantity.Value = Fields_Array[1];

            //Line_Type
            MyPOMT10.POLineStatus.Value = Fields_Array[2];

            //Line_Type
            MyPOMT10.POLineType.Value = Fields_Array[3];

            //Item_Num
            MyPOMT10.ItemNumber.Value = Fields_Array[4];

            //Prom_date
            MyPOMT10.PromisedDate.Value = "102013";
            MyPOMT10.NeededDate.Value = "021414";
            //MyPOMT10.PromisedDate.Value = Fields_Array[5];
            //MyPOMT10.NeededDate.Value = Fields_Array[5];

            //Blanket
            MyPOMT10.IsBlanketOrNonBlanket.Value = Fields_Array[6];

            //PO_UM
            MyPOMT10.POLineUM.Value = Fields_Array[7];

            //Unit_price
            MyPOMT10.ItemUnitCost.Value = Fields_Array[8];


            #endregion


            #region Ejecucion
            CDFResponse = MyPOMT10.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT10, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT10", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                DumpErrorObject(MyPOMT10, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT10", Trans_Error_Msg);
                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT11(string FSTI_fields, string Ammalgamma_User)
        {
            POMT11 MyPOMT11 = new POMT11();

            DBinUseFlag = false;
            #region Campos
            //PO_Number, PO_Line, Line_QTY, Line_Status, Item_Num, StartDate, EndDate, Unit_price, PromDate, Line_Type
            //0        , 1       , 2      , 3          , 4       , 5        , 6      , 7         , 8       , 9

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT11.PONumber.Value = Fields_Array[0];

            //PO_Line
            MyPOMT11.POLineNumber.Value = Fields_Array[1];

            //Line_QTY
            MyPOMT11.LineItemOrderedQuantity.Value = Fields_Array[2];

            //Line_Status
            MyPOMT11.POLineStatus.Value = Fields_Array[3];

            //Item_Num
            MyPOMT11.ItemNumber.Value = Fields_Array[4];

            //Star Date
            MyPOMT11.NeededDate.Value = Fields_Array[5];

            //End Date
            MyPOMT11.PromisedDate.Value = Fields_Array[6];

            //Unit_price
            MyPOMT11.ItemUnitCost.Value = Fields_Array[7];

            //Prom_date
            MyPOMT11.PromisedDateOld.Value = Fields_Array[8];

            //Line_Type
            MyPOMT11.POLineSubType.Value = Fields_Array[9];


            #endregion

            #region Ejecucion
            CDFResponse = MyPOMT11.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT11, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT11", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                DumpErrorObject(MyPOMT11, itemError);

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT11", Trans_Error_Msg);
                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT16(string FSTI_fields, string Ammalgamma_User)
        {
            POMT16 MyPOMT16 = new POMT16();

            DBinUseFlag = false;
            #region Campos
            //PO_Number, PO_Line, Item_Num, Promissed Date
            //0        , 1      , 2       , 3      

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT16.PONumber.Value = Fields_Array[0];

            //PO_Line
            MyPOMT16.POLineNumber.Value = Fields_Array[1];

            //Item_Num
            MyPOMT16.ItemNumber.Value = Fields_Array[2];

            //Promissed Date
            MyPOMT16.PromisedDateOld.Value = Fields_Array[3];

            //MyPOMT16.s

            #endregion


            #region Ejecucion
            CDFResponse = MyPOMT16.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT16, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT16", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT16", Trans_Error_Msg);
                DumpErrorObject(MyPOMT16, itemError);

                return false;
            }
            #endregion
        }
        public bool AmalgammaFSTI_POMT12(string FSTI_fields, string Ammalgamma_User)
        {
            POMT12 MyPOMT12 = new POMT12();

            DBinUseFlag = false;
            #region Campos
            //PO_Number, PO_Line, Item_Num, Original Promissed Date, single delivery line, Line Satus, Original Promissed Date, QTY 
            //0        , 1      , 2       , 3                      , 4                   , 5         , 6                      , 7

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT12.PONumber.Value = Fields_Array[0];

            //PO_Line
            MyPOMT12.POLineNumber.Value = Fields_Array[1];

            //Item_Num
            MyPOMT12.ItemNumber.Value = Fields_Array[2];

            //New Promissed Date
            MyPOMT12.PromisedDateOld.Value = Fields_Array[3];

            //single delivery line 
            MyPOMT12.POLineSubType.Value = Fields_Array[4];

            //Line Satus
            //MyPOMT12.POLineStatus.Value = "5";
            MyPOMT12.POLineStatus.Value = Fields_Array[5];

            //Original Promissed Date
            MyPOMT12.PromisedDate.Value = Fields_Array[6];

            //QTY
            try
            {
                float qty = float.Parse(Fields_Array[7]);
                MyPOMT12.LineItemOrderedQuantity.Value = qty.ToString();
            }
            catch
            {
            }

            #endregion


            #region Ejecucion
            CDFResponse = MyPOMT12.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT12, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", Trans_Error_Msg);
                DumpErrorObject(MyPOMT12, itemError);

                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT12_UPD(string FSTI_fields, string Ammalgamma_User)
        {
            POMT12 MyPOMT12 = new POMT12();
            DBinUseFlag = false;
            #region Campos
            //PO_Number, PO_Line, Item_Num, New Promissed Date, single delivery line, Line Satus, Original Promissed Date, QTY 
            //0        , 1      , 2       , 3                 , 4                   , 5         , 6                      , 7

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT12.PONumber.Value = Fields_Array[0];

            //PO_Line
            MyPOMT12.POLineNumber.Value = Fields_Array[1];

            //Item_Num
            MyPOMT12.ItemNumber.Value = Fields_Array[2];

            //New Promissed Date
            MyPOMT12.PromisedDate.Value = Fields_Array[3];

            //single delivery line 
            MyPOMT12.POLineSubType.Value = Fields_Array[4];

            //Line Satus
            //MyPOMT12.POLineStatus.Value = "5";
            MyPOMT12.POLineStatus.Value = Fields_Array[5];

            //Original Promissed Date
            MyPOMT12.PromisedDateOld.Value = Fields_Array[6];

            //QTY
            try
            {
                float qty = float.Parse(Fields_Array[7]);
                MyPOMT12.LineItemOrderedQuantity.Value = qty.ToString();
            }
            catch
            {
            }

            #endregion


            #region Ejecucion
            CDFResponse = MyPOMT12.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT12, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", Trans_Error_Msg);
                DumpErrorObject(MyPOMT12, itemError);

                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_POMT12_UPQ(string FSTI_fields, string Ammalgamma_User)
        {
            POMT12 MyPOMT12 = new POMT12();
            DBinUseFlag = false;
            #region Campos
            //PO_Number, PO_Line, Item_Num, New Promissed Date, single delivery line, Line Satus, Original Promissed Date, QTY 
            //0        , 1      , 2       , 3                 , 4                   , 5         , 6                      , 7

            string[] Fields_Array = FSTI_fields.Split(',');

            //PO_Number
            MyPOMT12.PONumber.Value = Fields_Array[0];

            //PO_Line
            MyPOMT12.POLineNumber.Value = Fields_Array[1];

            //Item_Num
            MyPOMT12.ItemNumber.Value = Fields_Array[2];

            //New Promissed Date
            MyPOMT12.PromisedDate.Value = Fields_Array[3];

            //single delivery line 
            MyPOMT12.POLineSubType.Value = Fields_Array[4];

            //Line Satus
            //MyPOMT12.POLineStatus.Value = "5";
            MyPOMT12.POLineStatus.Value = Fields_Array[5];

            //Original Promissed Date
            MyPOMT12.PromisedDateOld.Value = Fields_Array[6];

            //QTY
            try
            {
                float qty = float.Parse(Fields_Array[7]);
                MyPOMT12.LineItemOrderedQuantity.Value = qty.ToString();
            }
            catch
            {
            }

            #endregion


            #region Ejecucion
            CDFResponse = MyPOMT12.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyPOMT12, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                FSTI_Log(CDFResponse, Ammalgamma_User, "POMT12", Trans_Error_Msg);
                DumpErrorObject(MyPOMT12, itemError);

                return false;
            }
            #endregion

        }
        public bool AmalgammaFSTI_INVA01(string FSTI_fields, string Ammalgamma_User)
        {
            #region Fields

            INVA01 MyINVA01 = new INVA01();
            DBinUseFlag = false;

            //Item, Stk, Bin, Ic, DocNum, AC, Adj_Qty, RC, InvOffAcc, UM
            //   0,   1,   2,  3,      4,  5,       6,  7,         8,  9

            string[] Fields_Array = FSTI_fields.Split(',');

            MyINVA01.ItemNumber.Value = Fields_Array[0];
            MyINVA01.Stockroom.Value = Fields_Array[1];
            MyINVA01.Bin.Value = Fields_Array[2];
            MyINVA01.InventoryCategory.Value = Fields_Array[3];
            MyINVA01.DocumentNumber.Value = Fields_Array[4];
            MyINVA01.ActionCode.Value = Fields_Array[5];
            MyINVA01.AdjustQuantity.Value = Fields_Array[6];
            MyINVA01.Reason.Value = Fields_Array[7];
            MyINVA01.InventoryOffsetMasterAccountNumber.Value = Fields_Array[8];
            MyINVA01.ItemUM.Value = Fields_Array[9];


            #endregion

            #region Ejecucion
            CDFResponse = MyINVA01.GetString(TransactionStringFormat.fsCDF);

            if (_fstiClient.ProcessId(MyINVA01, null))
            {
                //
                FSTI_ErrorMsg = "";
                Trans_Error_Msg = "";

                FSTI_Log(CDFResponse, Ammalgamma_User, "INVA01", "");

                return true;
            }
            else
            {
                // failure, retrieve the error object 
                // and then dump the information in the list box
                itemError = null;
                itemError = _fstiClient.TransactionError;
                Trans_Error_Msg = itemError.Description;
                FSTI_Log(CDFResponse, Ammalgamma_User, "INVA01", Trans_Error_Msg);
                DumpErrorObject(MyINVA01, itemError);

                return false;
            }
            #endregion
        }
        public bool AmalgammaFSTI_PICK08(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            PICK08 MyPICK08 = new PICK08();

            try
            {
                //MO_NO, MO_LN, PART NO, Lot, STK, BIN, QTY, PtUse, OpNum
                //    0,     1,       2,   3,   4,   5,   6,     7,     8
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS

                //Order Type
                MyPICK08.OrderType.Value = "M";

                //Issue Type
                MyPICK08.IssueType.Value = "I";

                //MO_NO
                MyPICK08.OrderNumber.Value = Fields_Array[0];

                //MO_LN
                MyPICK08.LineNumber.Value = Fields_Array[1];

                //Com Typ/Ln# Typ 
                MyPICK08.ComponentLineType.Value = "N";

                try
                {
                    //Pt Use 
                    MyPICK08.PointOfUseID.Value = Fields_Array[7];

                    //OperationSeqNumber
                    MyPICK08.OperationSequenceNumber.Value = Fields_Array[8];
                    if (MyPICK08.OperationSequenceNumber.Value.Length < 3)
                    {
                        while(MyPICK08.OperationSequenceNumber.Value.Length<3)
                        {
                            MyPICK08.OperationSequenceNumber.Value = "0"+MyPICK08.OperationSequenceNumber.Value;
                        }
                    }
                }
                catch
                {
                    //Pt Use 
                    MyPICK08.PointOfUseID.Value = "0000";

                    //OperationSeqNumber
                    MyPICK08.OperationSequenceNumber.Value = "000";
                }

                //Item Number
                MyPICK08.ItemNumber.Value = Fields_Array[2];

                //STK
                MyPICK08.Stockroom.Value = Fields_Array[4];

                //BIN
                MyPICK08.Bin.Value = Fields_Array[5];

                //Inventory Category
                //MyPICK08.InventoryCategory.Value = "O";

                //Lot No
                MyPICK08.LotNumber.Value = Fields_Array[3];

                //QTY
                MyPICK08.IssuedQuantity.Value = Fields_Array[6];

                //Quantity Type
                MyPICK08.QuantityType.Value = "I";

                #endregion
                /////////
                CDFResponse = MyPICK08.GetString(TransactionStringFormat.fsCDF);



                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyPICK08, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK08", "");
                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyPICK08, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK08", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "PICK08", ex.Message);
                return false;
            }
        }

        #endregion

        #region Shipping Transaction
        public bool AmalgammaFSTI_SHIP_PICK18(string FSTI_fields, string Ammalgamma_User)
        {
            //Transaction Object
            PICK18 MyPICK18 = new PICK18();
            //IMTR01 MyIMTR01 = new IMTR01();

            try
            {
                //CO, CO_LN, PN, STK_FROM, BIN_FROM, IC, QTY, LOT
                // 0,     1,  2,        3,        4,  5,   6,   7
                string[] Fields_Array = FSTI_fields.Split(',');

                //CO
                MyPICK18.OrderNumber.Value = Fields_Array[0];

                //CO LN
                MyPICK18.LineNumber.Value = Fields_Array[1];

                //Item Number
                MyPICK18.ItemNumber.Value = Fields_Array[2];

                //STK
                MyPICK18.Stockroom.Value = Fields_Array[3];

                //BIN
                MyPICK18.Bin.Value = Fields_Array[4];

                //QTY
                MyPICK18.IssuedQuantity.Value = Fields_Array[6];

                //LOT
                MyPICK18.LotNumber.Value = Fields_Array[7];

                ////

                MyPICK18.IssueType.Value = "I";
                MyPICK18.OrderType.Value = "C";
                MyPICK18.PointOfUseID.Value = "";

                MyPICK18.StockroomShipping.Value = "EL";
                MyPICK18.BinShipping.Value = "SHIP";

                ////

                #region coments
                ////from

                //string[] LocFrom = Fields_Array[1].Split('-');
                //string BinFrom = "";

                //if (LocFrom.Count() > 2)
                //{
                //    for (int i = 1; i < LocFrom.Count(); i++)
                //    {
                //        if (i == 1)
                //        {
                //            BinFrom += LocFrom[i];
                //        }
                //        else
                //        {
                //            BinFrom += "-" + LocFrom[i];
                //        }
                //    }
                //}
                //else
                //{
                //    BinFrom = LocFrom[1];
                //}

                //MyPICK18.StockroomFrom.Value = LocFrom[0];
                //MyPICK18.BinFrom.Value = BinFrom;

                //MyPICK18.InventoryCategoryFrom.Value = Fields_Array[2];

                ////to

                ////IntemNumber,STK-BINFrom,InvCatFrom,STK-BINTo,InvCatTo,Qty

                //string[] LocTo = Fields_Array[3].Split('-');
                //string BinTo = "";

                //if (LocTo.Count() > 2)
                //{
                //    for (int i = 1; i < LocTo.Count(); i++)
                //    {
                //        if (i == 1)
                //        {
                //            BinTo += LocTo[i];
                //        }
                //        else
                //        {
                //            BinTo += "-" + LocTo[i];
                //        }
                //    }
                //}
                //else
                //{
                //    BinTo = LocTo[1];
                //}


                //MyPICK18.StockroomTo.Value = LocTo[0];
                //MyPICK18.BinTo.Value = BinTo;

                //MyPICK18.InventoryCategoryTo.Value = Fields_Array[4];

                ////QTY
                //MyPICK18.InventoryQuantity.Value = Fields_Array[5];

                ////Lot Tracing
                //#region LotTracing
                //string LotCheck = "SELECT IsLotTraced FROM  FS_Item WHERE (ItemNumber = '" + Fields_Array[0] + "')";

                //string NEW_LOT = "";

                //try
                //{
                //    NEW_LOT = Fields_Array[6].Trim();
                //}
                //catch
                //{
                //    NEW_LOT = "";
                //}

                //if ("Y" == DBMNG_FS.Execute_Scalar(LotCheck))
                //{
                //    MyPICK18.LotIdentifier.Value = "E";
                //    MyPICK18.LotNumberFrom.Value = NEW_LOT;

                //    try
                //    {
                //        if (Fields_Array[7].Trim() != "")
                //        {
                //            MyPICK18.LotNumberTo.Value = Fields_Array[7].Trim();// lot to
                //        }
                //        else
                //        {
                //            MyPICK18.LotNumberTo.Value = NEW_LOT;
                //        }
                //    }
                //    catch
                //    {
                //        MyPICK18.LotNumberTo.Value = NEW_LOT;
                //    }

                //}
                //else
                //{
                //    MyPICK18.LotIdentifier.Value = "N";
                //}

                //#endregion
                /////
                #endregion

                CDFResponse = MyPICK18.GetString(TransactionStringFormat.fsCDF);

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyPICK18, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK18", "");

                    //string Stage_Package = "SELECT CustomerID FROM FS_COHeader WHERE (CONumber = '" + Fields_Array[0] + "')";
                    //string CUST_ID = DBMNG_FS.Execute_Scalar(Stage_Package);

                    //if (Ammalgamma_User.Contains("VB - "))
                    //{
                    //    string Stage_Package_Q = "INSERT INTO _CAP_VB_STAGE_SHIPPING (CO,LN,ITEM_PN,QTY,VB_USER,CUST_ID) " +
                    //        "VALUES ('" + Fields_Array[0] + "','" + Fields_Array[1] + "','" + Fields_Array[2] + "'," + Fields_Array[6] +
                    //        ",'" + Ammalgamma_User.Replace("VB - ", "") + "','" + CUST_ID + "')";

                    //    DBMNG_FS.Execute_Command(Stage_Package_Q);
                    //}
    
                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyPICK18, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK18", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log( ex.Message, Ammalgamma_User, "PICK18","");
                return false;
            }
        }
        public bool AmalgammaFSTI_SHIP02(string FSTI_fields, string Ammalgamma_User)
        { 
            //Transaction Object
            //PICK18 MyPICK18 = new PICK18();
            SHIP02 MySHIP02 = new SHIP02();

            try
            {
                //CO, SHIP_NO, SHIP_REF, CO_LN_NO, QTY, LOT, ITEM_NO
                // 0,       1,        2,        3,   4,   5,       6
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS
                //Issue Type
                MySHIP02.IssueType.Value = "I";

                //Manual Shipment No
                MySHIP02.FirstTransaction.Value = "M";

                //CO
                MySHIP02.CONumber.Value = Fields_Array[0];
                
                //SHIPNO
                MySHIP02.ShipmentNumber.Value = Fields_Array[1];

                //SHIP REF
                MySHIP02.ShipmentReference.Value = Fields_Array[2];

                //CO LN
                MySHIP02.COLineNumber.Value = Fields_Array[3];

                //CO LINE TYPE
                MySHIP02.LineType.Value = "C";

                //QTY
                MySHIP02.ShippedQuantity.Value = Fields_Array[4];

                //STK-BIN
                MySHIP02.Stockroom.Value = "EL";
                MySHIP02.Bin.Value = "SHIP";

                //IC
                MySHIP02.InventoryCategory.Value = "S";

                //LOT
                MySHIP02.LotNumber.Value = Fields_Array[5];

                #endregion
                /////////
                CDFResponse = MySHIP02.GetString(TransactionStringFormat.fsCDF);

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MySHIP02, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP02", "");

                    string Stage_Package = "SELECT CustomerID FROM FS_COHeader WHERE (CONumber = '" + Fields_Array[0] + "')";
                    string CUST_ID = DBMNG_FS.Execute_Scalar(Stage_Package);

                    if (Ammalgamma_User.Contains("VB - "))
                    {
                        //CO, SHIP_NO, SHIP_REF, CO_LN_NO, QTY, LOT, ITEM_NO
                        // 0,       1,        2,        3,   4,   5,       6
                        string Stage_Package_Q = "INSERT INTO _CAP_VB_STAGE_SHIPPING (CO,LN,ITEM_PN,QTY,VB_USER,CUST_ID,SHIP_NO) " +
                            "VALUES ('" + Fields_Array[0] + "','" + Fields_Array[3] + "','" + Fields_Array[6] + "'," + Fields_Array[4] +
                            ",'" + Ammalgamma_User.Replace("VB - ", "") + "','" + CUST_ID + "','" + Fields_Array[1] + "')";

                        //DBMNG_FS.Execute_Command(Stage_Package_Q);
                    }
    

                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MySHIP02, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP02", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log(ex.Message, Ammalgamma_User, "SHIP02", "error");
                return false;
            }
        }
        public bool AmalgammaFSTI_SHIP06(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            SHIP06 MySHIP06 = new SHIP06();

            try
            {
                //CO_NO, ShipNo, CO_Ln#, LnItemQty, PartNo, PackType, PackWgt, PackNum, PackagesInSeries
                //    0,      1,      2,         3,      4,        5,       6,       7,                8       

                string[] Fields_Array = FSTI_fields.Split(',');
                #region FIELDS
                //Issue Type
                MySHIP06.IssueType.Value = "I";

                //First Transaction
                MySHIP06.FirstTransaction.Value = "N";

                //CO Number
                MySHIP06.CONumber.Value = Fields_Array[0];

                //Shipment No
                MySHIP06.ShipmentNumber.Value = Fields_Array[1];

                //CO Ln No
                MySHIP06.COLineNumber.Value = Fields_Array[2];

                //Package Number
                MySHIP06.PackageNumber.Value = Fields_Array[7]; 

                //Package in series
                MySHIP06.PackagesInSeries.Value = Fields_Array[8]; 

                //Package Type
                MySHIP06.PackageType.Value = Fields_Array[5];

                //Line Item Qty
                MySHIP06.LineItemQuantity.Value = Fields_Array[3];

                //Package Weight
                MySHIP06.PackageWeight.Value = Fields_Array[6];


                #endregion
                /////////
                CDFResponse = MySHIP06.GetString(TransactionStringFormat.fsCDF);
                
                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MySHIP06, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP06", "");
                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MySHIP06, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP06", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "SHIP06", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_SHIP13(string FSTI_fields, string Ammalgamma_User)
        {
            SHIP13 MySHIP13 = new SHIP13();

            try
            {
                //CO_NO, ShipNo, CO_Ln#, SerialNo, LotNO, 
                //    0,      1,      2,        3,     4,  

                string[] Fields_Array = FSTI_fields.Split(',');
                #region FIELDS

                //Action Identifier
                MySHIP13.ActionIdentifier.Value = "A";
                
                //CO Number
                MySHIP13.CONumber.Value = Fields_Array[0];

                //Shipment No
                MySHIP13.ShipmentNumber.Value = Fields_Array[1];

                //CO Ln No
                MySHIP13.COLineNumber.Value = Fields_Array[2];

                //StartingCounter
                MySHIP13.StartingCounter.Value = Fields_Array[3];

                //LotNumber
                MySHIP13.LotNumber.Value = Fields_Array[4];


                #endregion
                /////////
                CDFResponse = MySHIP13.GetString(TransactionStringFormat.fsCDF);



                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MySHIP13, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP13", "");
                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MySHIP13, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "SHIP13", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "SHIP13", ex.Message);
                return false;
            }
 
        }
        #endregion

        #region serials
        public bool AmalgammaFSTI_MOMT00(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            MOMT00 MyMOMT00 = new MOMT00();

            try
            {
                //MO_NO, LABEL_QTY, PART NO,
                //    0,         1,       2,
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS

                //MO_Number
                MyMOMT00.MONumber.Value = Fields_Array[0];


                #endregion
                /////////
                CDFResponse = MyMOMT00.GetString(TransactionStringFormat.fsCDF);

                string VerifyMO_Q = "SELECT FS_MOHeader.MONumber, FS_MOLine.MOLineNumber FROM FS_MOHeader INNER JOIN " +
                    " FS_MOLine ON FS_MOHeader.MOHeaderKey = FS_MOLine.MOHeaderKey " +
                    " WHERE FS_MOHeader.MONumber = '" + Fields_Array[0] + "')";

                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyMOMT00, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "MOMT00", "");

                    AmalgammaFSTI_MOMT06(FSTI_fields, Ammalgamma_User);

                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyMOMT00, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "MOMT00", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "MOMT00", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_MOMT06(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            MOMT06 MyMOMT06 = new MOMT06();

            try
            {
                //MO_NO, LABEL_QTY, PART NO,
                //    0,         1,       2,
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS

                //MO_Number
                MyMOMT06.MONumber.Value = Fields_Array[0];

                //Order QTY
                MyMOMT06.ItemOrderedQuantity.Value = Fields_Array[1];

                //Ln# Sta
                MyMOMT06.MOLineStatus.Value = "4";

                //Ln# Type
                MyMOMT06.MOLineType.Value = "R";

                //Item
                MyMOMT06.ItemNumber.Value = Fields_Array[2];

                //Start Date
                MyMOMT06.StartDate.Value = DateTime.Now.ToString("MMddyy");

                //Schedule Date
                MyMOMT06.ScheduledDate.Value = DateTime.Now.ToString("MMddyy");

                #endregion
                /////////
                CDFResponse = MyMOMT06.GetString(TransactionStringFormat.fsCDF);



                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyMOMT06, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "MOMT06", "");



                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyMOMT06, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "MOMT00", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "MOMT00", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_PICK00(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            PICK00 MyPICK00 = new PICK00();

            try
            {
                //MO_NO, MO_LN, LABEL_QTY, PART NO,
                //    0,     1,         2,       3,
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS

                //MO_Number
                MyPICK00.OrderNumber.Value = Fields_Array[0];

                //MO_LINE #
                MyPICK00.LineNumber.Value = Fields_Array[1];

                //Item
                MyPICK00.ItemNumber.Value = Fields_Array[3];
                
                //Remaining QTY
                MyPICK00.RemainingRequiredQuantity.Value = Fields_Array[2];

                //Order Type
                MyPICK00.OrderType.Value = "M";

                //Issue Type
                MyPICK00.IssueType.Value = "I";

                //Component Line Type
                MyPICK00.ComponentLineType.Value = "N";

                //Point Of Use
                MyPICK00.PointOfUseID.Value = "0000";

                //Operation Seq
                MyPICK00.OperationSequenceNumber.Value = "000";

                #endregion
                /////////
                CDFResponse = MyPICK00.GetString(TransactionStringFormat.fsCDF);



                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyPICK00, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK00", "");



                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyPICK00, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK00", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "PICK00", ex.Message);
                return false;
            }
        }
        public bool AmalgammaFSTI_PICK12(string FSTI_fields, string Ammalgamma_User)
        {  //Transaction Object

            PICK12 MyPICK12 = new PICK12();

            try
            {
                //MO_NO, MO_LN, PART NO, Lot, STK, BIN, QTY 
                //    0,     1,       2,   3,   4,   5,   6
                string[] Fields_Array = FSTI_fields.Split(',');

                #region FIELDS

                //Order Type
                MyPICK12.OrderType.Value = "M";

                //Issue Type
                MyPICK12.IssueType.Value = "I";

                //MO_NO
                MyPICK12.OrderNumber.Value = Fields_Array[0];

                //MO_LN
                MyPICK12.LineNumber.Value = Fields_Array[1];

                //Com Typ/Ln# Typ 
                MyPICK12.ComponentLineType.Value = "N";

                try
                {
                    //Pt Use 
                    MyPICK12.PointOfUseID.Value = Fields_Array[7];

                    //OperationSeqNumber
                    MyPICK12.OperationSequenceNumber.Value = Fields_Array[8];
                }
                catch
                {
                    //Pt Use 
                    MyPICK12.PointOfUseID.Value = "0000";

                    //OperationSeqNumber
                    MyPICK12.OperationSequenceNumber.Value = "000";
                }


                //Item Number
                MyPICK12.ItemNumber.Value = Fields_Array[2];

                //STK
                MyPICK12.Stockroom.Value = Fields_Array[4];

                //BIN
                MyPICK12.Bin.Value = Fields_Array[5];

                //Inventory Category
                MyPICK12.InventoryCategory.Value = "O";

                //Lot No
                MyPICK12.LotNumber.Value = Fields_Array[3];

                //QTY
                MyPICK12.IssuedQuantity.Value = Fields_Array[6];

                //Quantity Type
                MyPICK12.QuantityType.Value = "I";

                #endregion
                /////////
                CDFResponse = MyPICK12.GetString(TransactionStringFormat.fsCDF);



                DBinUseFlag = false;
                if (_fstiClient.ProcessId(MyPICK12, null))
                {
                    //
                    FSTI_ErrorMsg = "";
                    Trans_Error_Msg = "";

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK12", "");
                    return true;
                }
                else
                {
                    // failure, retrieve the error object 
                    // and then dump the information in the list box
                    itemError = null;
                    itemError = _fstiClient.TransactionError;
                    Trans_Error_Msg = itemError.Description;
                    DumpErrorObject(MyPICK12, itemError);

                    FSTI_Log(CDFResponse, Ammalgamma_User, "PICK12", Trans_Error_Msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FSTI_Log("", Ammalgamma_User, "PICK12", ex.Message);
                return false;
            }
        }
        #endregion

        #endregion
    }

}
//"SHIP02","MT","07/09/2015","014:06:39","0","C","B","C","I","","E-135670","949956","","","","","002","C","",100,"EL","SHIP","S","3S2015189565",""