using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Peraport.AdminPlugin.Models;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.PPService;
using System.Web;
using System.Data;
using ExcelDataReader;

namespace Nop.Plugin.Peraport.AdminPlugin.Controllers
{
    public class PPProductController : BasePluginController
    {
        #region ctor
        public string klasor = @"C:\pics\";
        private IEncryptionService _encryptionService;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;

        public PPProductController(
            IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IAuthenticationService authService,
            ICustomerService customerService,
            IEncryptionService encryptionService,
            IWebHelper webHelper,
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IUrlRecordService urlRecordService,
            IPictureService pictureService
            )
        {
            _encryptionService = encryptionService;
            _customerService = customerService;
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _urlRecordService = urlRecordService;
            _pictureService = pictureService;
        }
        #endregion


        public bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }
        public static byte[] GetPhoto(string filePath)
        {

            bool isfile = false;
            string locfile = "";
            if (filePath.Substring(1, 1) == ":")
            {
                isfile = true;
            }

            bool eh = true;

            if (isfile) { locfile = filePath; }
            else
            {
                locfile = "c:\\temp\\trp1.jpg";
                WebClient wc = new WebClient();
                try
                {
                    wc.DownloadFile(filePath, locfile);
                }
                catch (WebException e)
                {
                    eh = false;
                }
            }
            if (eh)
            {
                try
                {
                    FileStream stream = new FileStream(
                        locfile, FileMode.Open, FileAccess.Read);
                    BinaryReader reader = new BinaryReader(stream);

                    byte[] photo = reader.ReadBytes((int)stream.Length);

                    reader.Close();
                    stream.Close();
                    return photo;

                }
                catch (Exception Ex)
                {
                    return null;
                }
            }
            else return null;
        }
        public List<String> AddProductPictures(ErpProduct erp, Product p, string PicsFolder)
        {//
            int j = 1;
            List<string> results = new List<string>();
            results.Add(erp.NAME + "->");
            var ExistPictures = _pictureService.GetPicturesByProductId((int)p.Id);
            results.Add("Kayıtlı " + ExistPictures.Count + " resim var.");
            string[] FileNames = Directory.GetFiles(PicsFolder).Where(x => x.Contains(erp.BARCODE)).ToArray();
            results.Add("Klasörde " + FileNames.Length + " resim var.");
            foreach (var item in FileNames)
            {
                byte[] photo = GetPhoto(item);

                if (photo == null) { }
                else
                {
                    bool photoExist = false;
                    foreach (var itemx in ExistPictures)
                    {
                        if (itemx.PictureBinary == photo)
                        {
                            photoExist = true;
                            break;
                        }
                    }
                    if (photoExist)
                    {
                        results.Add(" > Resim : " + item + " Zaten Var.");
                    }
                    else
                    {
                        var pic = _pictureService.InsertPicture(photo, "image/jpeg", _pictureService.GetPictureSeName(erp.NAME), null, erp.NAME, true);

                        if (pic.Id > 0)
                        {
                            _productService.InsertProductPicture(new ProductPicture
                            {
                                ProductId = p.Id,
                                PictureId = pic.Id,
                                DisplayOrder = j
                            });
                            j++;
                            results.Add(" + Resim : " + item + " Eklendi.");
                        }
                        else
                        {
                            results.Add(" - Resim : " + item + " Eklenemedi.");
                        }
                    }
                }
            }
            return results;
        }

        public void AddPicture(byte[] photo, string Product_Name, string seoFileName) //Kullanılmıyor
        {
            _pictureService.InsertPicture(photo, "image/jpeg", seoFileName, Product_Name, Product_Name, true);
        }


        public List<String> AddCategoryPictures(ErpProduct erp, Product p, string PicsFolder)
        {//
            int j = 1;
            List<string> results = new List<string>();
            results.Add(erp.NAME + "->");
            var ExistPictures = _pictureService.GetPicturesByProductId((int)p.Id);
            results.Add("Kayıtlı " + ExistPictures.Count + " resim var.");
            string[] FileNames = Directory.GetFiles(PicsFolder).Where(x => x.Contains(erp.BARCODE)).ToArray();
            results.Add("Klasörde " + FileNames.Length + " resim var.");
            foreach (var item in FileNames)
            {
                byte[] photo = GetPhoto(item);

                if (photo == null) { }
                else
                {
                    bool photoExist = false;
                    foreach (var itemx in ExistPictures)
                    {
                        if (itemx.PictureBinary == photo)
                        {
                            photoExist = true;
                            break;
                        }
                    }
                    if (photoExist)
                    {
                        results.Add(" > Resim : " + item + " Zaten Var.");
                    }
                    else
                    {
                        var pic = _pictureService.InsertPicture(photo, "image/jpeg", _pictureService.GetPictureSeName(erp.NAME), null, erp.NAME, true);

                        if (pic.Id > 0)
                        {
                            _productService.InsertProductPicture(new ProductPicture
                            {
                                ProductId = p.Id,
                                PictureId = pic.Id,
                                DisplayOrder = j
                            });
                            j++;
                            results.Add(" + Resim : " + item + " Eklendi.");
                        }
                        else
                        {
                            results.Add(" - Resim : " + item + " Eklenemedi.");
                        }
                    }
                }
            }
            return results;
        }

        [AdminAuthorize]
        public ActionResult UpdateProductDescription()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/UpdateProductDescription.cshtml");
        }

        [HttpPost]
        public ActionResult UpdateProductDescription(HttpPostedFileBase excelFile)
        {
            ResultModel r = new ResultModel();
            r.LOGS = new List<string>();
            try
            {
                //Dosya kontrolü
                if (excelFile == null
                || excelFile.ContentLength == 0)
                {
                    ViewBag.Error = "Lütfen dosya seçimi yapınız.";

                    return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/UpdateProductDescription.cshtml");
                }
                else
                {
                    if (excelFile.FileName.EndsWith("xls")
                    || excelFile.FileName.EndsWith("xlsx"))
                    {
                        string path = Server.MapPath("~/Content/" + excelFile.FileName);
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                        excelFile.SaveAs(path);

                        FileStream stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read);
                        IExcelDataReader excelReader;
                        if (Path.GetExtension(path).ToUpper() == ".XLS")
                        {
                            excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                        }
                        else
                        {
                            excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        }
                        List<ResultModel> localList = new List<ResultModel>();
                        while (excelReader.Read())
                        {
                            localList.Add(new ResultModel { NAME = excelReader.GetString(0), NOTE = excelReader.GetString(1) });
                        }

                        //Okuma bitiriliyor.
                        excelReader.Close();

                        //using Excel = Microsoft.Office.Interop.Excel; //Kullanmak için bunu using e ekle
                        ////////+Exceli açıyoruz
                        //////Excel.Application application = new Excel.Application();
                        //////r.LOGS.Add("5");
                        //////Excel.Workbook workbook = application.Workbooks.Open(path);
                        //////r.LOGS.Add("6");
                        //////Excel.Worksheet worksheet = workbook.ActiveSheet;
                        //////r.LOGS.Add("7");
                        //////Excel.Range range = worksheet.UsedRange;
                        //////r.LOGS.Add("8");
                        //////List<ResultModel> localList = new List<ResultModel>();

                        //////for (int i = 1; i <= range.Rows.Count; i++)
                        //////{
                        //////    localList.Add(new ResultModel { NAME = ((Excel.Range)range.Cells[i, 1]).Text, NOTE = ((Excel.Range)range.Cells[i, 2]).Text });
                        //////}
                        //////application.Quit();


                        ViewBag.ListDetay = UpdateProductsDecriptionProcess(localList);
                        ViewBag.Result = r;
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/UpdateProductDescription.cshtml");
                    }
                    else
                    {
                        ViewBag.Error = "Dosya tipiniz yanlış, lütfen '.xls' yada '.xlsx' uzantılı dosya yükleyiniz.";
                        ViewBag.Result = r;
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/UpdateProductDescription.cshtml");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Result = r;
                ViewBag.Error = "Hata :" + ex.Message;
                return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/UpdateProductDescription.cshtml");
            }
        }

        public List<ResultModel> UpdateProductsDecriptionProcess(List<ResultModel> model)////match LastOrDefault
        {
            ResultModel errorProduct = new ResultModel();
            try
            {
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var csErp = client.GetGeneralSettings("CS_ERP");
                var matchList = client.getMatchingList("BASE_STR", "Product", model.Select(x => x.NAME).ToArray());
                foreach (var item in model)
                {
                    errorProduct = item;
                    var match = matchList.Where(x => x.BASE_STR == item.NAME).LastOrDefault();
                    if (match != null)
                    {
                        var product = _productService.GetProductById((int)match.MATCH_ID);
                        product.FullDescription = item.NOTE;
                        _productService.UpdateProduct(product);
                        item.LOGS = new List<string> { "Ok" };
                    }
                    else
                        item.LOGS = new List<string> { "Eşleşme Bulunamadı." };
                }
                return model;
            }
            catch (Exception ex)
            {
                if (errorProduct != null && errorProduct.NAME != "") errorProduct.LOGS = new List<string> { "Hata :" + ex.Message };
                return model;
            }
        }

        [AdminAuthorize]
        public ActionResult Index()
        {
            EksikEslestirmeleriTamamla();
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/Index.cshtml");
        }

        [AdminAuthorize]
        public ActionResult IndexPost()
        {

            return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/IndexPost.cshtml");
        }

        [HttpPost]
        public ActionResult IndexPost(HttpPostedFileBase excelFile)
        {
            try
            {
                //Dosya kontrolü
                if (excelFile == null
                || excelFile.ContentLength == 0)
                {
                    ViewBag.Error = "Lütfen dosya seçimi yapınız.";

                    return View();
                }
                else
                {
                    //Dosyanın uzantısı xls ya da xlsx ise;
                    if (excelFile.FileName.EndsWith("xls")
                    || excelFile.FileName.EndsWith("xlsx"))
                    {
                        //Seçilen dosyanın nereye kaydedileceği belirtiliyor.
                        string path = Server.MapPath("~/Content/" + excelFile.FileName);

                        //Dosya kontrol edilir, varsa silinir.
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }

                        //Excel path altına kaydedilir.
                        excelFile.SaveAs(path);
                    }
                }
                ViewBag.Test = "Test Ok";
                return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/IndexPost.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.Test = "Test Error :" + ex.Message;
                return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/IndexPost.cshtml");
            }
        }

        [AdminAuthorize]
        public ActionResult ProductSync()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSync.cshtml");
        }

        public string AddCategory(string code, int pid, WSData[] rs)
        {
            string s = "";
            var Menu = rs.Where(x => x.Val11 == code).Select(x => new { MARKA = x.Val8, CATEGORY = x.Val9, ITEMCODE = x.Val11, MODEL = x.Val14 }).Distinct().ToList();
            int kategoriParentId = -1;
            int markaParentId = -1;
            int modelParentId = -1;
            int modelId = -1;
            Category _categoryUretici;
            Category _category;
            Category _category2;
            Category _category3;
            #region cat
            foreach (var item in Menu)
            {
                var NopCategories_ = _categoryService.GetAllCategories();

                _categoryUretici = NopCategories_.Where(x => x.Name == item.CATEGORY).FirstOrDefault();
                var tarih = DateTime.Now;
                if (_categoryUretici == null)
                {
                    _categoryUretici = new Category
                    {
                        Id = 0,
                        Name = item.CATEGORY,
                        Description = item.CATEGORY.ToLower(),
                        ParentCategoryId = 0,
                        Published = true,
                        DisplayOrder = 1,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_categoryUretici);
                    kategoriParentId = _categoryUretici.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = kategoriParentId, ProductId = pid });
                    _urlRecordService.SaveSlug(_categoryUretici, _categoryUretici.ValidateSeName(_categoryUretici.GetSeName(), _categoryUretici.Name, true), 0);

                    s += " " + _categoryUretici.Name + "Eklendi.";
                }
                else
                {
                    kategoriParentId = _categoryUretici.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == markaParentId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = markaParentId, ProductId = pid });
                    }
                }

                _category = NopCategories_.Where(x => x.Name == item.CATEGORY).FirstOrDefault();
                if (_category == null)
                {
                    _category = new Category
                    {
                        Id = 0,
                        Name = item.CATEGORY,
                        Description = item.CATEGORY.ToLower(),
                        ParentCategoryId = 0,
                        Published = true,
                        DisplayOrder = 1,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_category);
                    markaParentId = _category.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = markaParentId, ProductId = pid });
                    _urlRecordService.SaveSlug(_category, _category.ValidateSeName(_category.GetSeName(), _category.Name, true), 0);

                    s += " " + _category.Name + "Eklendi.";
                }
                else
                {
                    markaParentId = _category.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == markaParentId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = markaParentId, ProductId = pid });
                    }
                }

                _category2 = NopCategories_.Where(x => x.Name == item.MARKA && x.ParentCategoryId == markaParentId).FirstOrDefault();
                tarih = DateTime.Now;
                if (_category2 == null)
                {
                    _category2 = new Category
                    {
                        Id = 0,
                        Name = item.MARKA,
                        Description = item.MARKA.ToLower(),
                        ParentCategoryId = markaParentId,
                        Published = true,
                        DisplayOrder = 1,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_category2);
                    modelParentId = _category2.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = modelParentId, ProductId = pid });
                    _urlRecordService.SaveSlug(_category2, _category2.ValidateSeName(_category2.GetSeName(), _category2.Name, true), 0);
                    s += " " + _category2.Name + "Eklendi.";

                }
                else
                {
                    modelParentId = _category2.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == modelParentId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = modelParentId, ProductId = pid });
                    }
                }

                _category3 = NopCategories_.Where(x => x.Name == item.MODEL && x.ParentCategoryId == modelParentId).FirstOrDefault();
                tarih = DateTime.Now;
                if (_category3 == null)
                {
                    _category3 = new Category
                    {
                        Id = 0,
                        Name = item.MODEL,
                        Description = item.MODEL.ToLower(),
                        ParentCategoryId = modelParentId,
                        Published = true,
                        DisplayOrder = 1,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_category3);
                    modelId = _category3.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = modelId, ProductId = pid });
                    _urlRecordService.SaveSlug(_category3, _category3.ValidateSeName(_category3.GetSeName(), _category3.Name, true), 0);
                    s += " " + _category3.Name + "Eklendi.";
                }
                else
                {
                    modelId = _category3.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == modelId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = modelId, ProductId = pid });
                    }
                }
            }
            #endregion cat
            return s;
        }

        public string AddCategoryYeni(string code, int pid, WSData[] rs)
        {
            string s = "";

            var Menu = rs.Where(x => x.Val11 == code).Select(x =>
            new
            {
                LEVEL1 = x.Val7,//BRAND,SP2  
                LEVEL2 = x.Val9,//CATEGORY,SP4
                LEVEL3 = x.Val10,//SUBCATEGORY,SP5
                LEVEL4 = x.Val8,//GSMBRAND,SP3
                LEVEL5 = x.Val14//GSMMODEL,CHARCODE.NAME
                , LEVELX = x.Val5
            }).Distinct().ToList();

            int l2ParentId = -1;
            int l3ParentId = -1;
            int l4ParentId = -1;
            int l5ParentId = -1;

            Category _categoryL1;
            Category _categoryL2;
            Category _categoryL3;
            Category _categoryL4;
            Category _categoryL5;
            #region cat
            foreach (var item in Menu)
            {
                var NopCategories_ = _categoryService.GetAllCategories();

                _categoryL1 = NopCategories_.Where(x => x.Name == item.LEVEL1 && x.DisplayOrder == 1).FirstOrDefault();
                var tarih = DateTime.Now;
                if (_categoryL1 == null)
                {
                    _categoryL1 = new Category
                    {
                        Id = 0,
                        Name = item.LEVEL1,
                        Description = item.LEVEL1.ToLower(),
                        ParentCategoryId = 0,
                        Published = true,
                        DisplayOrder = 1,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_categoryL1);
                    l2ParentId = _categoryL1.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL1.Id, ProductId = pid });
                    _urlRecordService.SaveSlug(_categoryL1, _categoryL1.ValidateSeName(_categoryL1.GetSeName(), _categoryL1.Name, true), 0);

                    s += " " + _categoryL1.Name + "Eklendi.";
                }
                else
                {
                    l2ParentId = _categoryL1.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == l2ParentId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL1.Id, ProductId = pid });
                        //_urlRecordService.SaveSlug(_categoryL1, _categoryL1.ValidateSeName(_categoryL1.GetSeName(), _categoryL1.Name, true), 0);
                    }
                }

                _categoryL2 = NopCategories_.Where(x => x.Name == item.LEVEL2 && x.ParentCategoryId == l2ParentId).FirstOrDefault();
                if (_categoryL2 == null)
                {
                    _categoryL2 = new Category
                    {
                        Id = 0,
                        Name = item.LEVEL2,
                        Description = item.LEVEL2.ToLower(),
                        ParentCategoryId = l2ParentId,
                        Published = true,
                        DisplayOrder = 2,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_categoryL2);
                    l3ParentId = _categoryL2.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL2.Id, ProductId = pid });
                    _urlRecordService.SaveSlug(_categoryL2, _categoryL2.ValidateSeName(_categoryL2.GetSeName(), _categoryL2.Name, true), 0);

                    s += " " + _categoryL2.Name + "Eklendi.";
                }
                else
                {
                    l3ParentId = _categoryL2.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == _categoryL2.Id).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL2.Id, ProductId = pid });
                    }
                }

                _categoryL3 = NopCategories_.Where(x => x.Name == item.LEVEL3 && x.ParentCategoryId == l3ParentId).FirstOrDefault();
                if (_categoryL3 == null)
                {
                    _categoryL3 = new Category
                    {
                        Id = 0,
                        Name = item.LEVEL3,
                        Description = item.LEVEL3.ToLower(),
                        ParentCategoryId = l3ParentId,
                        Published = true,
                        DisplayOrder = 3,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_categoryL3);
                    l4ParentId = _categoryL3.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL3.Id, ProductId = pid });
                    _urlRecordService.SaveSlug(_categoryL3, _categoryL3.ValidateSeName(_categoryL3.GetSeName(), _categoryL3.Name, true), 0);

                    s += " " + _categoryL3.Name + "Eklendi.";
                }
                else
                {
                    l4ParentId = _categoryL3.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == l4ParentId).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL3.Id, ProductId = pid });
                    }
                }

                _categoryL4 = NopCategories_.Where(x => x.Name == item.LEVEL4 && x.ParentCategoryId == l4ParentId).FirstOrDefault();
                if (_categoryL4 == null)
                {
                    _categoryL4 = new Category
                    {
                        Id = 0,
                        Name = item.LEVEL4,
                        Description = item.LEVEL4.ToLower(),
                        ParentCategoryId = l4ParentId,
                        Published = true,
                        DisplayOrder = 4,
                        PageSize = 15,
                        CategoryTemplateId = 1,
                        AllowCustomersToSelectPageSize = true,
                        PageSizeOptions = "15,30,100",
                        IncludeInTopMenu = true,
                        CreatedOnUtc = tarih,
                        UpdatedOnUtc = tarih
                    };
                    _categoryService.InsertCategory(_categoryL4);
                    l5ParentId = _categoryL4.Id;
                    _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL4.Id, ProductId = pid });
                    _urlRecordService.SaveSlug(_categoryL4, _categoryL4.ValidateSeName(_categoryL4.GetSeName(), _categoryL4.Name, true), 0);

                    s += " " + _categoryL4.Name + "Eklendi.";
                }
                else
                {
                    l5ParentId = _categoryL4.Id;
                    var cs = _categoryService.GetProductCategoriesByProductId(pid);
                    if (cs.Where(x => x.CategoryId == _categoryL4.Id).FirstOrDefault() == null)
                    {
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL4.Id, ProductId = pid });
                    }
                }

                if(item.LEVELX=="EKRKOR"|| item.LEVELX == "KILIF")
                {
                    _categoryL5 = NopCategories_.Where(x => x.Name == item.LEVEL5 && x.ParentCategoryId == l5ParentId).FirstOrDefault();
                    if (_categoryL5 == null)
                    {
                        _categoryL5 = new Category
                        {
                            Id = 0,
                            Name = item.LEVEL5,
                            Description = item.LEVEL5.ToLower(),
                            ParentCategoryId = l5ParentId,
                            Published = true,
                            DisplayOrder = 5,
                            PageSize = 15,
                            CategoryTemplateId = 1,
                            AllowCustomersToSelectPageSize = true,
                            PageSizeOptions = "15,30,100",
                            IncludeInTopMenu = true,
                            CreatedOnUtc = tarih,
                            UpdatedOnUtc = tarih
                        };
                        _categoryService.InsertCategory(_categoryL5);
                        _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL5.Id, ProductId = pid });
                        _urlRecordService.SaveSlug(_categoryL5, _categoryL5.ValidateSeName(_categoryL5.GetSeName(), _categoryL5.Name, true), 0);

                        s += " " + _categoryL5.Name + "Eklendi.";
                    }
                    else
                    {
                        var cs = _categoryService.GetProductCategoriesByProductId(pid);
                        if (cs.Where(x => x.CategoryId == _categoryL5.Id).FirstOrDefault() == null)
                        {
                            _categoryService.InsertProductCategory(new ProductCategory { CategoryId = _categoryL5.Id, ProductId = pid });
                        }
                    }
                }
            }
            #endregion cat
            return s;
        }

        public RESULT_S[] EksikEslestirmeleriTamamla()
        {
            List<string> result = new List<string>();
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var csErp = client.GetGeneralSettings("CS_ERP");

            var NopProducts = _productService.SearchProducts();
            var okList = client.getMatchingList("MATCH_ID", "Product", NopProducts.Select(x => x.Id.ToString()).ToArray());

            var ErpProducts = GetErpProductsFromErp();

            List<MATCHING_S> MatchList = new List<MATCHING_S>();
            foreach (var item in NopProducts.OrderBy(x => x.Id))
            {
                var il = okList.Where(x => x.MATCH_ID == item.Id).FirstOrDefault();
                if (il == null)
                {
                    var erpItem = ErpProducts.Where(x => x.CODE == item.UserAgreementText).FirstOrDefault();
                    if (erpItem != null)
                    {
                        MATCHING_S m = new MATCHING_S { TABLE_NAME = "Product", BASE_ID = erpItem.ID, MATCH_ID = item.Id, BASE_STR = erpItem.CODE };
                        MatchList.Add(m);
                    }
                    else
                        result.Add("Eşletirilemeyen Ürün Nop Id:" + item.Id + " ->" + item.Name);
                }

            }
            var xxx = client.MatchTwoRecord(MatchList.ToArray());
            return xxx;
        }

        public List<ErpProduct> GetErpProductsFromErp()
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var csErp = client.GetGeneralSettings("CS_ERP");

            string sorgu = ""
                /*1      2        3      4      5        6         7           8          9     10          11          12*/
                + " SELECT distinct I.LOGICALREF,I.CODE,I.NAME+' '+I.NAME3+' ' + I.CODE , I.VAT ,P.PRICE,CONVERT(VARCHAR(12), P.BEGDATE,104) BEGDATE , P.BEGTIME,CONVERT(VARCHAR(12), P.ENDDATE,104) ENDDATE,P.ENDTIME,UB.BARCODE,P.CURRENCY,P.UOMREF,I.SPECODE2"
                + " FROM[LogoDB].[dbo].[LG_517_PRCLIST] P"
                + " left join[LogoDB].[dbo].[LG_517_ITEMS] I ON P.CARDREF = I.LOGICALREF"
                + " left join[LogoDB].[dbo].[LG_517_CHARASGN] CA on CA.ITEMREF = I.LOGICALREF"
                + " left join[LogoDB].[dbo].[LG_517_CHARCODE] CC ON CA.CHARCODEREF=CC.LOGICALREF"
                + " left join[LogoDB].[dbo].[LG_517_UNITBARCODE] UB on UB.ITEMREF=I.LOGICALREF"
                + " inner join(select distinct  P.CARDREF, P.CURRENCY , max(P.BEGDATE) maxtar, max(ENDDATE) maxend, max(P.LOGICALREF) LOGREF"
                + " FROM[LogoDB].[dbo].[LG_517_PRCLIST] P"
                + " where BEGDATE<=convert(date, getdate()) and P.UOMREF=23 and P.PTYPE=2"
                + " group by P.CARDREF, P.CURRENCY) a on a.LOGREF=P.LOGICALREF and a.CARDREF=P.CARDREF and a.CURRENCY=p.CURRENCY and p.BEGDATE=a.maxtar and p.ENDDATE=a.maxend"
                + " WHERE I.ACTIVE=0 AND I.CARDTYPE = 1 and P.UOMREF=23 and UB.BARCODE is not null and (I.SPECODE4 is not null and I.SPECODE4<>'' ) and I.CYPHCODE='B2B' ORDER BY BEGDATE DESC"
                 ;
            var resultQuery = client.GetWSData(csErp.VALUE_STR, sorgu);
            List<ErpProduct> ErpProducts = new List<ErpProduct>();
            foreach (var item in resultQuery)
            {
                ErpProducts.Add(new ErpProduct
                {
                    ID = Convert.ToInt32(item.Val1),
                    CODE = item.Val2,
                    NAME = item.Val3,
                    KDV = Convert.ToInt16(item.Val4),
                    PRICE = Convert.ToDecimal(item.Val5),
                    BEGDATE = DateTime.ParseExact(item.Val6, "dd.MM.yyyy", null),//Convert.ToDateTime(item.Val6),
                    ENDDATE = DateTime.ParseExact(item.Val8, "dd.MM.yyyy", null),//Convert.ToDateTime(item.Val8),
                    CURRENCY = Convert.ToInt16(item.Val11),
                    MANUFACTURE = item.Val13,
                    BARCODE = item.Val10
                });
            }
            return ErpProducts;
        }

        [AdminAuthorize]
        public ActionResult ProductSyncPartial(int ProcessType = 0)
        {
            List<ResultModel> rm = new List<ResultModel>();
            if (ProcessType == 0)
            {
                ViewBag.ProcessType = 1;
                return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", null);
            }
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var csErp = client.GetGeneralSettings("CS_ERP");

            switch (ProcessType)
            {
                case 1:
                    #region Product Add
                    try
                    {
                        var erpKat = ""
                        + " SELECT I.SPECODE AS SC,I.SPECODE2 AS SC2,I.SPECODE3 AS SC3,I.SPECODE4 AS SC4,I.SPECODE5 AS SC5,"
                        + " COALESCE(NULLIF(SPC.DEFINITION_, ''), I.SPECODE) AS SPECODE" //6
                        + " , COALESCE(NULLIF(SPC2.DEFINITION_, ''), I.SPECODE2) AS SPECODE2, "//7
                        + " COALESCE(NULLIF(SPC3.DEFINITION_, ''), I.SPECODE3) AS SPECODE3, "//8
                        + " COALESCE(NULLIF(SPC4.DEFINITION_, ''), I.SPECODE4) AS SPECODE4, "//9
                        + " COALESCE(NULLIF(SPC5.DEFINITION_, ''), I.SPECODE5) AS SPECODE5"//10
                        + " ,I.CODE AS ITEMCODE,I.NAME AS ITEMNAME"//11,12
                        + " , CC.CODE AS MODELCODE,CC.NAME AS MODELNAME"//13,14
                        + " FROM[LogoDB].[dbo].[LG_517_ITEMS] I"
                        + " left join[LogoDB].[dbo].[LG_517_SPECODES] SPC on I.SPECODE = SPC.SPECODE and SPC.SPETYP1=1"
                        + " left join[LogoDB].[dbo].[LG_517_SPECODES] SPC2 on I.SPECODE2 = SPC2.SPECODE and SPC2.SPETYP2=1"
                        + " left join[LogoDB].[dbo].[LG_517_SPECODES] SPC3 on I.SPECODE3 = SPC3.SPECODE and SPC3.SPETYP3=1"
                        + " left join[LogoDB].[dbo].[LG_517_SPECODES] SPC4 on I.SPECODE4 = SPC4.SPECODE and SPC4.SPETYP4=1"
                        + " left join[LogoDB].[dbo].[LG_517_SPECODES] SPC5 on I.SPECODE5 = SPC5.SPECODE and SPC5.SPETYP5=1"
                        + " left join[LogoDB].[dbo].[LG_517_CHARASGN] CA on CA.ITEMREF = I.LOGICALREF"
                        + " left join[LogoDB].[dbo].[LG_517_CHARCODE] CC ON CA.CHARCODEREF=CC.LOGICALREF"
                        + " WHERE I.CARDTYPE = 1 and I.ACTIVE=0 and (I.SPECODE4 is not null and I.SPECODE4<>'' ) and I.CYPHCODE='B2B' and I.SPECODE2<>'DEMO'";
                        var resultCat = client.GetWSData(csErp.VALUE_STR, erpKat);

                        var ErpProducts = GetErpProductsFromErp();
                        var NopProducts = _productService.SearchProducts(showHidden: true);
                        var NopCategories = _categoryService.GetAllCategories();
                        var NopManufactures = _manufacturerService.GetAllManufacturers();
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Nop Product :" + NopProducts.Count() + " Nop Category :" + NopCategories.Count() + " Nop Üretici :" + NopManufactures.Count() });
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Erpden gelen Product Sayısı :" + ErpProducts.Count() });

                        rm.Add(new ResultModel { NAME = "-", NOTE = "Başlıyoruz...." });
                        int k = 0;
                        List<MATCHING_S> MatchList = new List<MATCHING_S>();
                        foreach (var item in ErpProducts.OrderBy(x => x.ID))
                        {
                            try
                            {
                                NopProducts = _productService.SearchProducts(showHidden: true);//Yayında olmayan kayıtlar da gelsin
                                NopCategories = _categoryService.GetAllCategories();
                                NopManufactures = _manufacturerService.GetAllManufacturers();

                                var _Product = NopProducts.Where(x => x.UserAgreementText == item.CODE).FirstOrDefault();
                                if (_Product == null)
                                {//Ürün yoksa
                                    k++;
                                    var tarih = DateTime.Now;
                                    _Product = new Product
                                    {
                                        Id = item.ID,
                                        Name = item.NAME,
                                        Sku = "Adet",
                                        Published = true,
                                        UpdatedOnUtc = tarih,
                                        CreatedOnUtc = tarih,
                                        ProductTypeId = 5,
                                        VisibleIndividually = true,
                                        TaxCategoryId = 1,
                                        AllowCustomerReviews = false,
                                        OrderMaximumQuantity = 1000,
                                        OrderMinimumQuantity = 1,
                                        Price = item.PRICE,
                                        ManageInventoryMethodId = 1,
                                        MinStockQuantity=5,
                                        LowStockActivityId = 2,
                                        DisplayStockAvailability = true,
                                        DisplayStockQuantity = true,
                                        IsFreeShipping = true,
                                        IsShipEnabled = true,
                                        UserAgreementText = item.CODE,
                                        AdminComment=item.CODE
                                    };

                                    _productService.InsertProduct(_Product);
                                    /*Kategori Bölümü*/
                                    //string acresult = AddCategory(item.CODE, _Product.Id, resultCat);
                                    string acresult = AddCategoryYeni(item.CODE, _Product.Id, resultCat);

                                    #region manufacturer

                                    var _manufacturer = NopManufactures.Where(x => x.Name == item.MANUFACTURE).FirstOrDefault();
                                    if (_manufacturer == null) _manufacturer = new Manufacturer
                                    {
                                        Id = 0,
                                        Name = item.MANUFACTURE,
                                        Published = true,
                                        DisplayOrder = 1,
                                        PageSize = 15,
                                        AllowCustomersToSelectPageSize = true,
                                        PageSizeOptions = "15,30,100",
                                        CreatedOnUtc = tarih,
                                        UpdatedOnUtc = tarih
                                    };
                                    var productManufacturer = new ProductManufacturer { Manufacturer = _manufacturer, ManufacturerId = _manufacturer.Id, Product = _Product, ProductId = _Product.Id };
                                    _manufacturerService.InsertProductManufacturer(productManufacturer);
                                    #endregion

                                    MATCHING_S m = new MATCHING_S { TABLE_NAME = "Product", BASE_ID = item.ID, MATCH_ID = _Product.Id, BASE_STR = item.CODE };
                                    MatchList.Add(m);
                                    rm.Add(new ResultModel { NAME ="+ " +_Product.Name, NOTE = "Ürün Eklendi. " });
                                    if(acresult.Length>5) rm.Add(new ResultModel { NAME = "+ Kategori", NOTE = acresult });

                                    //var picLogs = AddProductPictures(item, _Product, klasor);
                                    //rm.Add(new ResultModel { NAME = "Resim", LOGS = picLogs });

                                    #region urlRecord
                                    var seName = _Product.ValidateSeName(_Product.GetSeName(), _Product.Name, true);
                                    _urlRecordService.SaveSlug(_Product, seName, 0);
                                    _urlRecordService.SaveSlug(_manufacturer, _manufacturer.ValidateSeName(_manufacturer.GetSeName(), _manufacturer.Name, true), 0);
                                    #endregion

                                }
                                else
                                {
                                    rm.Add(new ResultModel { NAME = "  "+_Product.Name, NOTE = " Ürün Kayıtlı" });
                                }
                                new MATCHING_S { TABLE_NAME = "Product", BASE_ID = item.ID, MATCH_ID = _Product.Id, BASE_STR = "" };
                            }
                            catch (Exception e)
                            {
                                rm.Add(new ResultModel { NAME = item.NAME, NOTE = item.BARCODE + " Hata Oluştu." });
                            }

                        }
                        // Ürünler eklendi. şimdi eşleştirme yapılacak
                        var xxx = client.MatchTwoRecord(MatchList.ToArray());
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Bitirdik. Eklenen Ürün Sayısı : " + k });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                    catch (Exception Ex)
                    {
                        rm.Add(new ResultModel { NAME = "", NOTE = "Hata : " + Ex });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }

                #endregion
                case 2://match yok. UserAgreementText
                    #region Stock Update
                    try
                    {
                        string sorgu = ""
                        + " SELECT ITEMS.LOGICALREF, ITEMS.CODE, ITEMS.NAME, STINVTOT.INVENNO AS DEPO, SUM(STINVTOT.ONHAND) AS MIKTAR"
                        + " FROM[LogoDB].[dbo].LV_517_01_STINVTOT AS STINVTOT"
                        + " LEFT OUTER JOIN[LogoDB].[dbo].LG_517_ITEMS AS ITEMS ON STINVTOT.STOCKREF = ITEMS.LOGICALREF"
                        + " WHERE(ITEMS.CARDTYPE = 1) AND STINVTOT.INVENNO = -1" /* Tüm Depolar için -1 */
                        + " GROUP BY ITEMS.LOGICALREF,ITEMS.CODE, ITEMS.NAME, STINVTOT.INVENNO, ITEMS.CARDTYPE"
                        + " HAVING(SUM(STINVTOT.ONHAND) <> 0)"
                        ;
                        var resultQuery = client.GetWSData(csErp.VALUE_STR, sorgu);


                        List<ErpProduct> ErpProducts = new List<ErpProduct>();
                        foreach (var item in resultQuery)
                        {
                            ErpProducts.Add(new ErpProduct
                            {
                                ID = Convert.ToInt32(item.Val1),
                                CODE = item.Val2,
                                STOK = Convert.ToInt32(item.Val5)
                            });
                        }
                        //Erp den gelen ürünler ve stokları elimizde
                        //şimdi güncel stokları nop a yazacağız
                        var NopProducts = _productService.SearchProducts(showHidden: true);
                        List<Product> taskProduct = new List<Product>();

                        //ErpProducts = ErpProducts.Where(x => x.CODE == "RK070320001").ToList();

                        rm.Add(new ResultModel { NAME = "-", NOTE = "Nop Product :" + NopProducts.Count() });
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Erpden gelen Product Sayısı :" + resultQuery.Count() });
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Başlıyoruz...." });
                        var nopGuncellistesi = new List<int>();
                        foreach (var item in ErpProducts)
                        {
                            var nopEs = NopProducts.Where(x => x.UserAgreementText == item.CODE).ToArray();
                            if (nopEs != null)
                            {
                                //Stok güncellemesi yapılacak.
                                foreach (var itemx in nopEs)
                                {
                                    itemx.StockQuantity = (int)item.STOK;
                                    itemx.Published = true;
                                    taskProduct.Add(itemx);
                                    nopGuncellistesi.Add(itemx.Id);
                                }
                            }
                        }
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Stok Güncelleme Sayısı :" + taskProduct.Count() });
                        if (taskProduct.Count > 0) _productService.UpdateProducts(taskProduct);

                        /*6 dan küçük stokları yayından kaldır*/
                        var csNop = client.GetGeneralSettings("CS_Nop");
                        //var guncellenmeyenler = NopProducts.Select(x => x.Id).Except(nopGuncellistesi).ToArray();
                        var gg = NopProducts.Where(x => x.StockQuantity < 6).Select(x => x.Id).ToArray();
                        var str = String.Join(", ", gg.ToArray());
                        var nopQuery = "UPDATE [NopComLocal].[dbo].[Product] SET StockQuantity=0,Published=0,LowStockActivityId=2 where Id in ("+str+");";
                        var r = client.SqlExecuteNonQuery(csNop.VALUE_STR, nopQuery);
                        /**/
                        rm.Add(new ResultModel { NAME = "-", NOTE = "İşlem Başarılı. Güncellenen Stok Sayısı :" + taskProduct.Count() });

                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                    catch (Exception Ex)
                    {
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Hata oluştur. İşlem Başarısız. Hata :" + Ex });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                #endregion
                case 3:////match yok. UserAgreementText
                    #region Price Update
                    try
                    {
                        string sorgu = ""
                            + " SELECT distinct  I.LOGICALREF,I.CODE,I.VAT ,P.PRICE,P.BEGDATE,P.BEGTIME,P.ENDDATE,P.ENDTIME,P.CURRENCY,P.UOMREF"
                            + " , COALESCE(NULLIF(P.CAPIBLOCK_MODIFIEDDATE, null), P.CAPIBLOCK_CREADEDDATE) AS PRICEDATE"
                            + " FROM[LogoDB].[dbo].[LG_517_PRCLIST] P"
                            + " left join[LogoDB].[dbo].[LG_517_ITEMS] I ON P.CARDREF = I.LOGICALREF"
                            + " left join[LogoDB].[dbo].[LG_517_CHARASGN] CA on CA.ITEMREF = I.LOGICALREF"
                            + " left join[LogoDB].[dbo].[LG_517_CHARCODE] CC ON CA.CHARCODEREF=CC.LOGICALREF"
                            + " inner join(select distinct  P.CARDREF, P.CURRENCY , max(P.BEGDATE) maxtar,max(ENDDATE) maxend,max(P.LOGICALREF) LOGREF"
                            + " FROM[LogoDB].[dbo].[LG_517_PRCLIST] P"
                            + " where BEGDATE<=convert(date, getdate()) and P.UOMREF=23 and P.PTYPE=2"
                            + " group by P.CARDREF, P.CURRENCY) a on a.LOGREF=P.LOGICALREF and a.CARDREF=P.CARDREF and a.CURRENCY=p.CURRENCY and p.BEGDATE=a.maxtar and p.ENDDATE=a.maxend"
                            + " WHERE I.CARDTYPE = 1 and P.UOMREF=23 ORDER BY I.CODE"
                            ;
                        var resultQuery = client.GetWSData(csErp.VALUE_STR, sorgu);
                        List<ErpProduct> ErpProducts = new List<ErpProduct>();
                        foreach (var item in resultQuery)
                        {
                            ErpProducts.Add(new ErpProduct
                            {
                                ID = Convert.ToInt32(item.Val1),
                                CODE = item.Val2,
                                NAME = "",
                                KDV = Convert.ToInt16(item.Val3),
                                PRICE = Convert.ToDecimal(item.Val4),
                                BEGDATE = Convert.ToDateTime(item.Val5),
                                ENDDATE = Convert.ToDateTime(item.Val7),
                                CURRENCY = Convert.ToInt16(item.Val9),
                                BARCODE = "",
                                MODIFIEDDATE = Convert.ToDateTime(item.Val11)//DateTime.ParseExact(item.Val11, "dd.MM.yyyy hh:mm:ss", null)
                            });
                        }
                        //Erp den gelen ürünler ve fiyatları elimizde
                        //şimdi nop ile karşılaştırma yapıp, nop.modify < erp.modify olanları güncelleyeceğiz 
                        var NopProducts = _productService.SearchProducts(showHidden: true);
                        List<Product> taskProduct = new List<Product>();

                        rm.Add(new ResultModel { NAME = "-", NOTE = "Nop Product :" + NopProducts.Count() });
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Erpden gelen Product Sayısı :" + resultQuery.Count() });
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Başlıyoruz...." });

                        foreach (var item in ErpProducts)
                        {
                            var nopEs = NopProducts.Where(x => x.UserAgreementText == item.CODE).ToArray();
                            if (nopEs != null)
                            {
                                foreach (var itemx in nopEs)
                                {
                                   // if (item.MODIFIEDDATE >= itemx.UpdatedOnUtc)
                                    {//Fiyat güncellemesi yapılacak.
                                        rm.Add(new ResultModel { NAME = item.CODE, NOTE = " Eski Fiyat : (" + itemx.Price+")    <-->  Yeni Fiyat :("+item.PRICE + ")  Ürün : " + item.CODE});
                                        itemx.OldPrice = 0;
                                        itemx.Price = item.PRICE;
                                        taskProduct.Add(itemx);
                                    }
                                }
                            }
                        }
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Fiyat Değişim Sayısı :" + taskProduct.Count() });
                        if (taskProduct.Count > 0) _productService.UpdateProducts(taskProduct);
                        rm.Add(new ResultModel { NAME = "-", NOTE = "İşlem Başarılı. Güncellenen Fiyat Sayısı :" + taskProduct.Count() });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                    catch (Exception Ex)
                    {
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Hata oluştur. İşlem Başarısız. Hata :" + Ex });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                #endregion
                case 4:// Match LastOrDefault
                    #region Pictures Update
                    try
                    {

                        string sorgu = ""
                        + " SELECT distinct I.LOGICALREF,I.CODE,I.NAME,UB.BARCODE"
                        + " FROM[LogoDB].[dbo].[LG_517_ITEMS] I"
                        + " left join[LogoDB].[dbo].[LG_517_UNITBARCODE] UB on UB.ITEMREF = I.LOGICALREF"
                        + " WHERE I.CARDTYPE = 1 and UB.BARCODE is not null"
                        ;
                        var resultQuery = client.GetWSData(csErp.VALUE_STR, sorgu);

                        var NopProducts = _productService.SearchProducts(showHidden: true);
                        var matchList = client.getMatchingList("MATCH_ID", "Product", NopProducts.Select(x => x.Id.ToString()).ToArray());

                        foreach (var _product in NopProducts)
                        {
                            var match = matchList.Where(x => x.MATCH_ID == _product.Id).LastOrDefault();
                            if (match != null)
                            {
                                var barcode = resultQuery.Where(x => x.Val1 == match.BASE_ID.ToString()).FirstOrDefault();
                                if (barcode.Val1 != null)
                                {
                                    var erp_product = new ErpProduct
                                    {
                                        ID = Convert.ToInt32(barcode.Val1),
                                        CODE = barcode.Val2,
                                        NAME = barcode.Val3,
                                        BARCODE = barcode.Val4
                                    };
                                    try
                                    {
                                        //eski resimleri sil
                                        var p = _productService.GetProductPicturesByProductId(_product.Id);
                                        foreach (var _p in p)
                                        {
                                            _productService.DeleteProductPicture(_p);
                                        }
                                        var picLogs = AddProductPictures(erp_product, _product, klasor);
                                        rm.Add(new ResultModel { NAME = "OK PICS", NOTE = "AddProductPictures(" + erp_product.NAME + ")", LOGS = picLogs });
                                    }
                                    catch (Exception ex)
                                    {
                                        rm.Add(new ResultModel { NAME = "HATA PICS", NOTE = "AddProductPictures(" + erp_product.NAME + ") Hata:" + ex.Message });
                                    }

                                }
                                else
                                {
                                    rm.Add(new ResultModel { NAME = "X PICS", NOTE = " Ürün Erp de yok :" + _product.Name });
                                }

                            }
                            else
                            {
                                rm.Add(new ResultModel { NAME = "X PICS", NOTE = " Ürün Eşleştirmesi yok :" + _product.Name });
                            }
                        }
                        rm.Add(new ResultModel { NAME = "PICS", NOTE = "Resim Güncelleme işlemi tamamlandı." });

                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                    catch (Exception Ex)
                    {
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Hata oluştur. İşlem Başarısız. Hata :" + Ex });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                #endregion
                case 5://match var
                    #region Name Update
                    try
                    {
                        var ErpProducts = GetErpProductsFromErp();

                        //Erp den gelen ürünler elimizde
                        var NopProducts = _productService.SearchProducts(showHidden: true);
                        List<Product> taskProduct = new List<Product>();

                        rm.Add(new ResultModel { NAME = "->", NOTE = "Nop Product :" + NopProducts.Count() });
                        rm.Add(new ResultModel { NAME = "->", NOTE = "Erpden gelen Product Sayısı :" + ErpProducts.Count() });
                        rm.Add(new ResultModel { NAME = "->", NOTE = "Başlıyoruz...." });

                        var matchList = client.getMatchingList("MATCH_ID", "Product", NopProducts.Select(x => x.Id.ToString()).ToArray());

                        foreach (var _product in NopProducts)
                        {
                            var match = matchList.Where(x => x.MATCH_ID == _product.Id).LastOrDefault();
                            if (match != null)
                            {
                                ErpProduct _erpproduct = ErpProducts.Where(x => x.ID == match.BASE_ID).LastOrDefault();
                                if (_erpproduct != null)
                                {
                                    if (_product.Name == _erpproduct.NAME)
                                    {
                                        rm.Add(new ResultModel { NAME = "X Name", NOTE = " İsim Aynı. Değişiklik yok. :" + _product.Name });
                                    }
                                    else
                                    {
                                        _product.Name = _erpproduct.NAME;
                                        //_productService.UpdateProduct(_product);
                                        taskProduct.Add(_product);
                                        rm.Add(new ResultModel { NAME = _erpproduct.CODE, NOTE = " İsim Güncellendi. :" + _product.Name });
                                    }
                                }
                                else
                                {
                                    rm.Add(new ResultModel { NAME = "X", NOTE = " Ürün Erpde yok :" + _product.Name });
                                }
                            }
                            else
                            {
                                rm.Add(new ResultModel { NAME = "X", NOTE = " Ürün Eşleştirmesi yok :" + _product.Name });
                            }
                        }
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Ürün Güncelleme Sayısı :" + taskProduct.Count() });
                        if (taskProduct.Count > 0) _productService.UpdateProducts(taskProduct);
                        rm.Add(new ResultModel { NAME = "-", NOTE = "İşlem Başarılı. Güncellenen Ürün Sayısı :" + taskProduct.Count() });

                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                    catch (Exception Ex)
                    {
                        rm.Add(new ResultModel { NAME = "-", NOTE = "Hata oluştur. İşlem Başarısız. Hata :" + Ex });
                        return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", rm);
                    }
                #endregion
                default:
                    return View("~/Plugins/Peraport.AdminPlugin/Views/PPProduct/ProductSyncPartial.cshtml", null);
            }
        }
    }
}
