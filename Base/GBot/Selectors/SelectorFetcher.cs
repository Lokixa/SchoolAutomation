﻿using System;
using System.Collections.Generic;
using System.Reflection;
using GBot.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GBot
{
    public class SelectorFetcher
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;
        // private WebDriverWait firstWait;
        private NLog.Logger logger;
        private HashSet<Type> TypesToTraverse;
        const int POST_DEPTH = 30;

        public SelectorFetcher(IWebDriver Driver)
        {
            this.driver = Driver;
            wait = new WebDriverWait(Driver, new TimeSpan(0, 0, 0, 0, 500));
            // firstWait = new WebDriverWait(Driver, new TimeSpan(0, 0, 4));
            logger = NLog.LogManager.GetCurrentClassLogger();
            TypesToTraverse = new();
        }
        public bool Register<T>()
        {
            return TypesToTraverse.Add(typeof(T));
        }
        /// <summary>
        /// Populates T object with the fetched FromSelectors
        /// </summary>
        /// <param name="baseSelector">Base selector of item. Used in relative searches.</param>
        /// <param name="isXpath">Are selectors xpath</param>
        /// <typeparam name="T">Class with FromSelector attributes</typeparam>
        /// <returns>Populated T item</returns>
        public T Fill<T>() where T : new()
        {
            var fromSel = typeof(T).GetCustomAttribute<FromSelector>();
            return Fill<T>(fromSel.Selector);
        }
        internal T Fill<T>(string BaseSelector) where T : new()
        {
            bool isXpath = IsXpath(BaseSelector);
            var selAttr = typeof(T).GetCustomAttribute<FromSelector>();
            T fill = new T();

            Queue<(object, string)> queue = new();
            queue.Enqueue((fill, BaseSelector));
            while (queue.Count > 0)
            {
                (object @class, string baseSel) = queue.Dequeue();

                var props = @class.GetType().GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    var propSel = prop.GetCustomAttribute<FromXPath>();
                    if (propSel != null)
                    {
                        string selector = propSel.Selector;
                        if (propSel.InheritFromClass)
                        {
                            if (isXpath && propSel is not FromXPath)
                            {
                                throw new InvalidSelectorException("Class and properties have different types");
                            }
                            selector = BaseSelector + selector;
                        }

                        if (TypesToTraverse.Contains(prop.PropertyType))
                        {
                            if (prop.PropertyType == @class.GetType())
                            {
                                logger.Debug("Recursion on {0}", @class.GetType().Name);
                                continue;
                            }
                            var ctor = prop.PropertyType.GetConstructor(System.Type.EmptyTypes);
                            if (ctor == null)
                                throw new Exception("No parameterless constructor for " + prop.PropertyType.Name);
                            object childClass = ctor.Invoke(null);
                            prop.SetValue(@class, childClass);
                            queue.Enqueue(
                                (childClass, baseSel + selector)
                            );
                        }
                        else
                        {
                            IWebElement el = Fetch(selector, isXpath);
                            object value = Parse(el, prop.PropertyType);
                            prop.SetValue(@class, value);
                        }
                    }
                    else
                    {
                        if (prop.PropertyType == typeof(IWebElement))
                        {
                            prop.SetValue(@class, Fetch(baseSel, isXpath));
                        }
                    }
                }
            }

            return fill;
        }

        private bool IsXpath(string BaseSelector)
        {
            if (BaseSelector.Length == 0)
            {
                throw new ArgumentException(nameof(BaseSelector));
            }
            if (BaseSelector.Trim()[0] == '/') return true;
            return false;
        }

        private IWebElement Fetch(string Selector, bool IsXpath)
        {
            WebDriverWait waiter = wait;
            // logger.Trace("Fetching {0} with {1} timeout", Selector, waiter.Timeout);
            By by;
            if (IsXpath)
            {
                by = By.XPath(Selector);
            }
            else
            {
                by = By.CssSelector(Selector);
            }
            IWebElement el = waiter.Until(driver =>
               driver.FindElement(by)
            );
            waiter.Until(driver => el.Displayed);
            return el;
        }

        private object Parse(IWebElement Element, Type PropType)
        {
            object value = null;
            if (PropType == typeof(string))
            {
                value = Element.Text;
            }

            return value;
        }


        /// <summary>
        /// Enumerates from top of classroom: '{index}' in T's selectors
        /// </summary>
        /// <param name="Index">Index from top of classroom.</param>
        /// <param name="Found">
        /// Predicate used in searching for valid item.
        /// <br/>
        /// Default is item != null
        /// </param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(int Index, Predicate<T> Found = null) where T : new()
        {
            if (Found == null)
            {
                Found = item => item != null;
            }
            var type = typeof(T);
            FromSelector baseClass = type.GetCustomAttribute<FromSelector>();
            //TODO REFACTOR
            T item = default(T);
            for (int i = 1; Index >= 0;)
            {
                do
                {
                    string selector = baseClass.Selector.Replace("{index}", i.ToString());
                    // logger.Trace("Trying to fetch '{0}'", selector);
                    try
                    {
                        item = Fill<T>(selector);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        item = default(T);
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(ex);
                    }
                    i++;
                } while (!Found(item) && i < POST_DEPTH);
                Index--;
            }

            return item;
        }
        public T FindAfter<T>(T Item, int Times) where T : new()
        {
            T el = default(T);
            FromSelector baseClass = typeof(T).GetCustomAttribute<FromSelector>();
            bool isXpath = baseClass is FromXPath;
            int i = 1;
            bool found = false;
            // logger.Trace("Received {0}", Item);
            while (Times >= 0)
            {
                do
                {
                    string selector = baseClass.Selector.Replace("{index}", i.ToString());
                    // logger.Trace("Filling selector [ {0} ]", selector);
                    try
                    {
                        el = Fill<T>(selector);
                    }
                    catch //(Exception ex)
                    {
                        el = default(T);
                    }
                    i++;
                } while (el == null && i < POST_DEPTH);
                if (el.Equals(Item))
                {
                    found = true;
                }
                if (found)
                {
                    Times--;
                }
            }
            return el;
        }
    }
}