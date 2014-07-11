

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;


namespace Leem.Testify
{

    public class League
    {
        public League(string name)
        {
            _name = name;
            bValue = true;
            _divisions = new List<League>();
        }


        string _name;
        bool bValue;

        public string Name { get { return _name; } }
        public bool IsChecked
        {
            get { return true; }

            set
            {
                bValue = value;
            }

        }

        List<League> _divisions;
        public List<League> Divisions
        {
            get
            {
                return _divisions;
            }


        }


    }

    public class ListLeagueList : List<League>
    {
        public ListLeagueList()
        {
            League l;
            League d;
            League d1, d2;

            Add(l = new League("League A"));
            l.Divisions.Add((d = new League("Division A")));
            d.Divisions.Add(d1 = new League("Team I"));
            d1.Divisions.Add(d2 = new League("Team I.1.1"));
            d2.Divisions.Add(new League("Team I.1.2"));
            d2.Divisions.Add(new League("Team I.1.3"));
            d1.Divisions.Add(new League("Team I.2"));
            d1.Divisions.Add(new League("Team I.3"));


            d.Divisions.Add(new League("Team II"));
            d.Divisions.Add(new League("Team III"));
            d.Divisions.Add(new League("Team IV"));
            d.Divisions.Add(new League("Team V"));
            l.Divisions.Add((d = new League("Division B")));
            d.Divisions.Add(new League("Team Blue"));
            d.Divisions.Add(new League("Team Red"));
            d.Divisions.Add(new League("Team Yellow"));
            d.Divisions.Add(new League("Team Green"));
            d.Divisions.Add(new League("Team Orange"));
            l.Divisions.Add((d = new League("Division C")));
            d.Divisions.Add(new League("Team East"));
            d.Divisions.Add(new League("Team West"));
            d.Divisions.Add(new League("Team North"));
            d.Divisions.Add(new League("Team South"));
            Add(l = new League("League B"));
            l.Divisions.Add((d = new League("Division A")));
            d.Divisions.Add(new League("Team 1"));
            d.Divisions.Add(new League("Team 2"));
            d.Divisions.Add(new League("Team 3"));
            d.Divisions.Add(new League("Team 4"));
            d.Divisions.Add(new League("Team 5"));
            l.Divisions.Add((d = new League("Division B")));
            d.Divisions.Add(new League("Team Diamond"));
            d.Divisions.Add(new League("Team Heart"));
            d.Divisions.Add(new League("Team Club"));
            d.Divisions.Add(new League("Team Spade"));
            l.Divisions.Add((d = new League("Division C")));
            d.Divisions.Add(new League("Team Alpha"));
            d.Divisions.Add(new League("Team Beta"));
            d.Divisions.Add(new League("Team Gamma"));
            d.Divisions.Add(new League("Team Delta"));
            d.Divisions.Add(new League("Team Epsilon"));
        }

        //public League this[string name]
        //{
        //    get
        //    {
        //        foreach (League l in this)
        //            if (l.Name == name)
        //                return l;

        //        return null;
        //    }
        //}
    }
}