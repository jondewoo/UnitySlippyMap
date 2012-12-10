<?php

/*
echo "<pre>" . print_r($_REQUEST, true) . "</pre>";
echo "<pre>" . print_r($_SERVER, true) . "</pre>";
*/

define("KEY", "ArgkafZs0o_PGBuyg468RaapkeIQce996gkyCe8JN30MjY92zC_2hcgBU_rHVUwT");

$url = urldecode($_REQUEST["url"]);
//echo "<pre>" . $url . "</pre>";
$request = parse_url($url);
$query = array();

if (isset($request["query"]))
{
	parse_str($request["query"], $query);
}

if (isset($query["key"]) == false && isset($_REQUEST["key"]) == true)
{
	$query = $_REQUEST;
}

/*
echo "<pre>" . print_r($request, true) . "</pre>";
echo "<pre>" . print_r($query, true) . "</pre>";
*/

if (empty($query) || isset($query["key"]) === false || empty($query["key"]) === true || $query["key"] != KEY)
{
	header("HTTP/1.1 403 Forbidden");
	die('The key was not set or is incorrect. If you are trying to use this script outside of UnitySlippyMap testing purpose, please copy this script to your favorite web hosting and use your own key.');
}

header("Location: " . $url);

?>