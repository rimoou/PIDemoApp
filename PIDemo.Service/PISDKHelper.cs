using System.Collections.Frozen;
using PISDK;
using PISDKCommon;


namespace PIDemo.Service
{
    public class PISDKHelper
    {
        public PISDKHelper()
        {
        }

        public string Server { get; set; }
        public int Port { get; set; }
        private string _connectionString;
        private Server _myserver = null;


        // 打开数据库
        public Server PIOpen(string server, int port)
        {
            this.Server = server;

            PISDK.PISDK g_SDK = new PISDK.PISDKClass();

            //var ss = g_SDK.Servers;

            //foreach (Server s in ss) {
            //    Console.WriteLine(s.Name);
            //    Console.WriteLine(s.Path);
            //}


            _myserver = g_SDK.Servers[server];

            //if (myserver is null)
            //{
            //    myserver = g_SDK.Servers.Add(server, connectionString,"3");
            //}

            _myserver.ConnectTimeout = 10;
            _myserver.Timeout = 20;
            // _myserver.Port = 5450;
            _myserver.Port = port;

            try
            {
                if (!_myserver.Connected)
                {
                    _myserver.Open();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            Console.WriteLine("连接成功！");
            Console.WriteLine($"server名称：{_myserver.Name}");
            Console.WriteLine($"server id：{_myserver.ServerID}");
            Console.WriteLine($"server路径：{_myserver.Path}");
            Console.WriteLine($"server端口：{_myserver.Port}");
            Console.WriteLine($"当前用户：{_myserver.CurrentUser}");
            Console.WriteLine($"连接类型：{_myserver.ConnectionType}");
            Console.WriteLine($"PI版本：{_myserver.ServerVersion.Version}");
            Console.WriteLine($"服务器系统：{_myserver.ServerVersion.OSName}@{_myserver.ServerVersion.OSVersion}");
            Console.WriteLine();

            return _myserver;
        }

        //关闭连接
        public void PIClose()
        {
            if (!_myserver.Connected)
            {
                _myserver.Close();
            }
        }


        // 获取指定点位的实时快照
        public Point Pi_GetPointSnapshot(string tagName)
        {
            string value = string.Empty;
            Point p = new Point();
            try
            {
                if (_myserver == null || !_myserver.Connected)
                {
                    PIOpen(Server, Port); //打开数据库
                }

                PISDK.PIPoints piPoints = _myserver.PIPoints;
                PISDK.PIPoint snapShot = piPoints[tagName];
                PISDK.PIValue myValue = snapShot.Data.Snapshot;
                PISDK.PointAttributes pa = snapShot.PointAttributes;
                PISDKCommon._NamedValues nvs = pa.GetAttributes();

                p.TagName = tagName;
                p.Pvalue = myValue.Value.ToString();

                // 点位的值类型
                var pointType = snapShot.PointType;

                foreach (NamedValue namedValue in nvs)
                {
                    var n = namedValue.Name;
                    var v = namedValue.Value.ToString();

                    p.PointProperty.Add(n, v);
                }

                PointClass piclass = snapShot.PointClass;


                Object valIndex = 16;
                // p.Description = nvs.get_Item(ref valIndex).Value.ToString(); //获取点描述字段
                p.Description = p.PointProperty["descriptor"];


                var nn = piclass.Name;
                var realName = snapShot.Name;

                PITimeServer.PITime pi = myValue.TimeStamp;
                p.PdateTime = pi.LocalDate.ToString(); //时间戳   
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                PIClose();
            }

            return p;
        }


        /// <summary>
        /// 获取指定点位时间段内所有值
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<Point> Pi_GetPointValues(string tag, DateTime startTime, DateTime endTime)
        {
            List<Point> list = new List<Point>();
            try
            {
                if (_myserver == null || !_myserver.Connected)
                {
                    PIOpen(Server, Port);
                }

                PISDK.PIValues pvs = null;
                PISDK.PIPoint ipoint = _myserver.PIPoints[tag];
                pvs = ipoint.Data.RecordedValues(startTime, endTime, PISDK.BoundaryTypeConstants.btInside, "",
                    PISDK.FilteredViewConstants.fvRemoveFiltered, null);
                PISDK.PointAttributes pa = ipoint.PointAttributes;
                PISDKCommon._NamedValues nvs = pa.GetAttributes();

                Object valIndex = 16;
                string description = nvs.get_Item(ref valIndex).Value.ToString(); //获取点描述字段
                foreach (PISDK.PIValue pv in pvs)
                {
                    Point p = new Point();

                    PITimeServer.PITime pi = pv.TimeStamp;
                    p.PdateTime = pi.LocalDate.ToString(); //时间戳
                    p.Description = description; //描述字段
                    p.Pvalue = pv.Value.ToString(); //值
                    p.TagName = tag;
                    list.Add(p);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                PIClose();
            }

            return list;
        }

        /// <summary>
        /// 获取指定时间戳归档值
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string GetPointValue(string tag, DateTime dateTime)
        {
            if (_myserver == null || !_myserver.Connected)
            {
                PIOpen(Server, Port);
            }

            PIData pt = _myserver.PIPoints[tag].Data;
            PIValue v = pt.ArcValue(dateTime, RetrievalTypeConstants.rtAtOrBefore, null);
            dateTime = v.TimeStamp.LocalDate;
            string rev;
            if (v.Value.GetType().IsCOMObject)
            {
                DigitalState myDigState = (PISDK.DigitalState)v.Value;
                rev = myDigState.Name;
            }
            else
            {
                rev = v.Value.ToString();
            }

            return rev;
        }


        public bool CreatePoint(string pointName, PISDK.PointTypeConstants pointType,
            FrozenDictionary<string, string> attriDic)
        {
            try
            {
                if (_myserver == null || !_myserver.Connected)
                {
                    PIOpen(Server, Port); //打开数据库
                }

                var attri = new NamedValues();

                if (attriDic is not null && attriDic.Count > 0)
                {
                    foreach (var keyValuePair in attriDic)
                    {
                        attri.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }


                // attri.Add("pointsource", "C");
                // attri.Add("location1", 1);
                // attri.Add("compressing", true);
                // attri.Add("descriptor", "666888");

                var res = _myserver.PIPoints.Add(pointName, "classic", pointType, attri);

                var nvs = res.PointAttributes.GetAttributes();

                var point = new Point();
                point.TagName = res.Name;

                foreach (NamedValue namedValue in nvs)
                {
                    var n = namedValue.Name;
                    var v = namedValue.Value.ToString();

                    point.PointProperty.Add(n, v);
                }


                Console.WriteLine("成功创建点位！");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        /// <summary>
        /// 添加多个点位
        /// </summary>
        public bool CreateMutiPoint(string[] tagNames, PISDK.PointTypeConstants pointType,
            FrozenDictionary<string, string> attriDic)
        {
            try
            {
                if (_myserver == null || !_myserver.Connected)
                {
                    PIOpen(Server, Port); //打开数据库
                }

                IPIPoints2 IPIP2 = (IPIPoints2)_myserver.PIPoints;

                // NamedValues 集合以用于添加和移除点位
                var nvsAdd = new NamedValues();


                // 定义添加的点的属性
                var nvsTagAttr = new NamedValues();
                nvsTagAttr.Add("ptClassName", "classic");
                nvsTagAttr.Add("pointtype", pointType);

                if (attriDic is not null && attriDic.Count > 0)
                {
                    foreach (var keyValuePair in attriDic)
                    {
                        nvsTagAttr.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }


                // 添加标签
                foreach (var tagName in tagNames)
                {
                    nvsAdd.Add(tagName, nvsTagAttr);
                }


                PIErrors myErrors;
                PointList myPtlist;

                IPIP2.AddTags(nvsAdd, out myErrors, out myPtlist);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }


        public void UpdateValue(string? tagName, string? newVal, DataMergeConstants dataMerge)
        {
            try
            {
                if (_myserver == null || !_myserver.Connected)
                {
                    PIOpen(Server, Port); //打开数据库
                }

                var p = _myserver.PIPoints[tagName];
                
                
                p.Data.UpdateValue(newVal, 0, dataMerge);
                
                
             
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public void UpdateValues(string tagName)
        {
            var p = _myserver.PIPoints[tagName];
                
                
        
                
            var nvAttris = new NamedValues();

            nvAttris.Add("isBirthDay", 1);
             
            var newPv = new PIValues { ReadOnly = false };
            newPv.Add(0,12.44,nvAttris);
            newPv.Add(0,35124,nvAttris);
                
                
            p.Data.UpdateValues(newPv,  DataMergeConstants.dmErrorDuplicates);
        }
        
        
        
    }


    public class Point
    {
        //点位名称
        public string TagName { get; set; }

        //点位描述
        public string Description { get; set; }

        //时间戳
        public string PdateTime { get; set; }

        //点位值
        public string Pvalue { get; set; }


        public Dictionary<string, string> PointProperty { get; set; } = new();
    }
}